using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Page.Ui.Application.Auth.Interfaces;
using Page.Ui.Application.Common.Interfaces;
using Page.Ui.Domain.Auth.Entities;
using BC = BCrypt.Net.BCrypt;

namespace Page.Ui.Infrastructure.Auth.Services;

public sealed class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IRsaKeyService _rsaKeyService;

    public AuthService(
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IRsaKeyService rsaKeyService)
    {
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
        _rsaKeyService = rsaKeyService;
    }

    public async Task<string> GenerateAccessTokenAsync(string userId, string email, IEnumerable<string> roles)
    {
        var user = await _userManager.FindByIdAsync(userId);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Name, user?.Name ?? string.Empty)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var rsa = _rsaKeyService.GetPrivateKey();
        var key = new RsaSecurityKey(rsa);
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:Issuer"] ?? _configuration["JWT__Issuer"] ?? "PageUi",
            audience: _configuration["JWT:Audience"] ?? _configuration["JWT__Audience"] ?? "PageUiUser",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["JWT:AccessTokenExpirationMinutes"] ?? _configuration["JWT__AccessTokenExpirationMinutes"] ?? "120")),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GenerateRefreshTokenAsync(string userId, string remoteIpAddress)
    {
        var rawRefreshToken = RandomNumberGenerator.GetHexString(64);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            JwtId = Guid.NewGuid().ToString(),
            CreationDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(double.Parse(_configuration["JWT:RefreshTokenExpirationDays"] ?? "30")),
            Used = false,
            Invalidated = false,
            RemoteIpAddress = remoteIpAddress,
            HashedToken = HashToken(rawRefreshToken)
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return rawRefreshToken;
    }

    public async Task<(bool Success, string AccessToken, string RefreshToken)> RefreshAccessTokenAsync(string token, string remoteIpAddress)
    {
        var hashedToken = HashToken(token);
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.HashedToken == hashedToken);

        if (storedToken == null || storedToken.Invalidated || storedToken.Used || storedToken.ExpiryDate < DateTime.UtcNow)
        {
            if (storedToken != null && (storedToken.Invalidated || storedToken.Used))
            {
                var userTokens = _context.RefreshTokens.Where(rt => rt.UserId == storedToken.UserId);
                foreach (var userToken in userTokens)
                {
                    userToken.Invalidated = true;
                }

                await _context.SaveChangesAsync();
            }

            return (false, null!, null!);
        }

        storedToken.Used = true;
        await _context.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(storedToken.UserId);
        if (user == null)
        {
            return (false, null!, null!);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = await GenerateAccessTokenAsync(user.Id, user.Email ?? string.Empty, roles);
        var newRefreshToken = await GenerateRefreshTokenAsync(user.Id, remoteIpAddress);

        return (true, newAccessToken, newRefreshToken);
    }

    public async Task<bool> InvalidateRefreshTokenAsync(string token)
    {
        var hashedToken = HashToken(token);
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.HashedToken == hashedToken);

        if (storedToken == null)
        {
            return false;
        }

        storedToken.Invalidated = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<string?> VerifyPasswordResetCodeAsync(string email, string code)
    {
        var resetCode = await _context.PasswordResetCodes
            .Where(c => c.Email == email && c.ExpirationTime > DateTime.UtcNow && !c.IsVerified)
            .OrderByDescending(c => c.ExpirationTime)
            .FirstOrDefaultAsync();

        if (resetCode == null)
        {
            return null;
        }

        if (!BC.Verify(code, resetCode.Code))
        {
            return null;
        }

        var tempToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        resetCode.IsVerified = true;
        resetCode.TempToken = tempToken;
        resetCode.ExpirationTime = DateTime.UtcNow.AddMinutes(5);

        await _context.SaveChangesAsync();
        return tempToken;
    }

    public async Task<bool> ResetPasswordAsync(string email, string tempToken, string newPassword)
    {
        var resetCode = await _context.PasswordResetCodes
            .FirstOrDefaultAsync(c => c.Email == email &&
                                      c.TempToken == tempToken &&
                                      c.IsVerified &&
                                      c.ExpirationTime > DateTime.UtcNow);

        if (resetCode == null)
        {
            return false;
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return false;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            return false;
        }

        var resetCodes = _context.PasswordResetCodes.Where(c => c.Email == email);
        _context.PasswordResetCodes.RemoveRange(resetCodes);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAccountAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var refreshTokens = _context.RefreshTokens.Where(rt => rt.UserId == userId);
        _context.RefreshTokens.RemoveRange(refreshTokens);

        var resetCodes = _context.PasswordResetCodes.Where(c => c.Email == user.Email);
        _context.PasswordResetCodes.RemoveRange(resetCodes);

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return false;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    private static string HashToken(string token)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
