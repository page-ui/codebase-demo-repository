using System.Security.Claims;
using System.Security.Cryptography;
using HotChocolate;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Page.Ui.Application.Auth.Contracts;
using Page.Ui.Application.Auth.DTOs;
using Page.Ui.Application.Auth.Interfaces;
using Page.Ui.Application.Common.Interfaces;
using Page.Ui.Domain.Auth.Entities;
using Page.Ui.Presentation.Auth.GraphQl.Support;
using Page.Ui.Presentation.Common.Security;
using StackExchange.Redis;

namespace Page.Ui.Presentation.Auth.Services;

public sealed class AuthMutationWorkflow
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly IConnectionMultiplexer _redis;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly IAuthService _authService;
    private readonly ILogger<AuthMutationWorkflow> _logger;

    public AuthMutationWorkflow(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext context,
        IPublishEndpoint publishEndpoint,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IConnectionMultiplexer redis,
        IHttpContextAccessor? httpContextAccessor,
        IAuthService authService,
        ILogger<AuthMutationWorkflow> logger)
    {
        _userManager = userManager;
        _context = context;
        _publishEndpoint = publishEndpoint;
        _passwordHasher = passwordHasher;
        _redis = redis;
        _httpContextAccessor = httpContextAccessor;
        _authService = authService;
        _logger = logger;
    }

    public async Task<bool> RegisterAsync(RegisterInput input)
    {
        input.Email = AuthInputGuard.NormalizeEmail(input.Email);
        input.Name = input.Name.Trim();

        if (!AreSafe(input.Email, input.Name))
        {
            _logger.LogWarning("Auth register rejected due to unsafe input. Email={Email} RequesterIp={RequesterIp}", input.Email, GetRequesterIp());
            return false;
        }

        if (await IsRateLimitedAsync(
                "register",
                new AuthRateLimitRule(BuildRateLimitKey("register", "ip", GetRequesterIp()), 10, TimeSpan.FromMinutes(10)),
                new AuthRateLimitRule(BuildRateLimitKey("register", "email", input.Email), 5, TimeSpan.FromMinutes(10))))
        {
            return false;
        }

        var existingUser = await _userManager.FindByEmailAsync(input.Email);
        if (existingUser != null)
        {
            _logger.LogInformation("Auth register ignored because account already exists. Email={Email} RequesterIp={RequesterIp}", input.Email, GetRequesterIp());
            return false;
        }

        var code = RandomNumberGenerator.GetInt32(10000, 100000).ToString();
        var hashedCode = BCrypt.Net.BCrypt.HashPassword(code);
        var hashedPassword = _passwordHasher.HashPassword(new ApplicationUser { Email = input.Email }, input.Password);
        var now = DateTime.UtcNow;

        var pending = await _context.PendingRegistrations
            .FirstOrDefaultAsync(p => p.Email == input.Email);

        if (pending != null)
        {
            pending.Name = input.Name;
            pending.PasswordHash = hashedPassword;
            pending.VerificationCode = hashedCode;
            pending.ExpirationTime = now.AddHours(24);
            pending.LastResentAt = now;
        }
        else
        {
            pending = new PendingRegistration
            {
                Email = input.Email,
                Name = input.Name,
                PasswordHash = hashedPassword,
                VerificationCode = hashedCode,
                ExpirationTime = now.AddHours(24),
                LastResentAt = now
            };
            _context.PendingRegistrations.Add(pending);
        }

        var retroEmail = AuthRetroEmailBuilder.Build(input.Email, code, "EMAIL_VERIFICATION", "SYSTEM - REGISTRATION LOG");
        await _publishEndpoint.Publish(new AuthEmailRequested(
            input.Email,
            "SECURE_SYSTEM_MESSAGE: VERIFICATION_CODE",
            retroEmail));

        await _context.SaveChangesAsync();

        _logger.LogInformation("Auth register accepted and verification email queued. Email={Email} RequesterIp={RequesterIp}", input.Email, GetRequesterIp());
        return true;
    }

    public async Task<LoginResult?> LoginAsync(LoginInput input)
    {
        input.Email = AuthInputGuard.NormalizeEmail(input.Email);

        if (!AreSafe(input.Email))
        {
            _logger.LogWarning("Auth login rejected due to unsafe input. Email={Email} RequesterIp={RequesterIp}", input.Email, GetRequesterIp());
            return null;
        }

        if (await IsRateLimitedAsync(
                "login",
                new AuthRateLimitRule(BuildRateLimitKey("login", "ip", GetRequesterIp()), 30, TimeSpan.FromMinutes(5)),
                new AuthRateLimitRule(BuildRateLimitKey("login", "email-ip", $"{input.Email}:{GetRequesterIp()}"), 8, TimeSpan.FromMinutes(5))))
        {
            return null;
        }

        var user = await _userManager.FindByEmailAsync(input.Email);
        if (user == null)
        {
            _logger.LogInformation("Auth login failed because user was not found. Email={Email} RequesterIp={RequesterIp}", input.Email, GetRequesterIp());
            return null;
        }

        if (!user.EmailConfirmed)
        {
            _logger.LogInformation("Auth login failed because email is not confirmed. UserId={UserId} Email={Email}", user.Id, input.Email);
            return null;
        }

        var isValid = await _userManager.CheckPasswordAsync(user, input.Password);
        if (!isValid)
        {
            _logger.LogInformation("Auth login failed because password validation failed. UserId={UserId} Email={Email}", user.Id, input.Email);
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var requesterIp = GetRequesterIp();
        var accessToken = await _authService.GenerateAccessTokenAsync(user.Id, user.Email ?? string.Empty, roles);
        var refreshToken = await _authService.GenerateRefreshTokenAsync(user.Id, requesterIp);

        _logger.LogInformation("Auth login succeeded. UserId={UserId} Email={Email} RequesterIp={RequesterIp}", user.Id, input.Email, requesterIp);
        return new LoginResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    public async Task<LoginResult?> RefreshTokenAsync(string refreshToken)
    {
        refreshToken = AuthInputGuard.Normalize(refreshToken);

        if (!AreSafe(refreshToken))
        {
            _logger.LogWarning("Auth refresh-token rejected due to unsafe token input. RequesterIp={RequesterIp}", GetRequesterIp());
            return null;
        }

        if (await IsRateLimitedAsync("refresh", new AuthRateLimitRule(BuildRateLimitKey("refresh", "ip", GetRequesterIp()), 60, TimeSpan.FromMinutes(5))))
        {
            return null;
        }

        var requesterIp = GetRequesterIp();
        var result = await _authService.RefreshAccessTokenAsync(refreshToken, requesterIp);
        if (!result.Success)
        {
            _logger.LogInformation("Auth refresh-token failed. RequesterIp={RequesterIp}", requesterIp);
            return null;
        }

        _logger.LogInformation("Auth refresh-token succeeded. RequesterIp={RequesterIp}", requesterIp);
        return new LoginResult
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken
        };
    }

    public async Task<bool> ForgotPasswordRequestAsync(string email)
    {
        email = AuthInputGuard.NormalizeEmail(email);

        if (!AreSafe(email))
        {
            _logger.LogWarning("Auth forgot-password rejected due to unsafe input. Email={Email} RequesterIp={RequesterIp}", email, GetRequesterIp());
            return false;
        }

        if (await IsRateLimitedAsync(
                "forgot-password",
                new AuthRateLimitRule(BuildRateLimitKey("forgot-password", "ip", GetRequesterIp()), 10, TimeSpan.FromMinutes(10)),
                new AuthRateLimitRule(BuildRateLimitKey("forgot-password", "email", email), 5, TimeSpan.FromMinutes(15))))
        {
            return false;
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogInformation("Auth forgot-password ignored because email was not found. Email={Email} RequesterIp={RequesterIp}", email, GetRequesterIp());
            return false;
        }

        var code = RandomNumberGenerator.GetInt32(10000, 100000).ToString();
        var hashedCode = BCrypt.Net.BCrypt.HashPassword(code);

        _context.PasswordResetCodes.Add(new PasswordResetCode
        {
            Email = email,
            Code = hashedCode,
            ExpirationTime = DateTime.UtcNow.AddMinutes(15)
        });

        var retroEmail = AuthRetroEmailBuilder.Build(email, code, "PASSWORD RECOVERY", "SYSTEM - ACCESS LOG");
        await _publishEndpoint.Publish(new AuthEmailRequested(
            email,
            "SECURE_SYSTEM_MESSAGE: RECOVERY_CODE",
            retroEmail));

        await _context.SaveChangesAsync();
        _logger.LogInformation("Auth forgot-password accepted and recovery email queued. UserId={UserId} Email={Email}", user.Id, email);
        return true;
    }

    public async Task<string?> VerifyResetCodeAsync(string email, string code)
    {
        email = AuthInputGuard.NormalizeEmail(email);
        code = AuthInputGuard.Normalize(code);

        if (!AreSafe(email, code))
        {
            _logger.LogWarning("Auth verify-reset-code rejected due to unsafe input. Email={Email} RequesterIp={RequesterIp}", email, GetRequesterIp());
            return null;
        }

        if (await IsRateLimitedAsync(
                "verify-reset",
                new AuthRateLimitRule(BuildRateLimitKey("verify-reset", "ip", GetRequesterIp()), 30, TimeSpan.FromMinutes(10)),
                new AuthRateLimitRule(BuildRateLimitKey("verify-reset", "email-ip", $"{email}:{GetRequesterIp()}"), 10, TimeSpan.FromMinutes(10))))
        {
            return null;
        }

        var tempToken = await _authService.VerifyPasswordResetCodeAsync(email, code);
        _logger.LogInformation(
            "Auth verify-reset-code {Outcome}. Email={Email} RequesterIp={RequesterIp}",
            tempToken is null ? "failed" : "succeeded",
            email,
            GetRequesterIp());
        return tempToken;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordInput input)
    {
        input.Email = AuthInputGuard.NormalizeEmail(input.Email);
        input.Token = AuthInputGuard.Normalize(input.Token);

        if (!AreSafe(input.Email, input.Token))
        {
            _logger.LogWarning("Auth reset-password rejected due to unsafe input. Email={Email} RequesterIp={RequesterIp}", input.Email, GetRequesterIp());
            return false;
        }

        if (await IsRateLimitedAsync("reset-password", new AuthRateLimitRule(BuildRateLimitKey("reset-password", "ip", GetRequesterIp()), 15, TimeSpan.FromMinutes(15))))
        {
            return false;
        }

        var success = await _authService.ResetPasswordAsync(input.Email, input.Token, input.NewPassword);
        _logger.LogInformation(
            "Auth reset-password {Outcome}. Email={Email} RequesterIp={RequesterIp}",
            success ? "succeeded" : "failed",
            input.Email,
            GetRequesterIp());
        return success;
    }

    public async Task<bool> SignOutAsync(string refreshToken)
    {
        refreshToken = AuthInputGuard.Normalize(refreshToken);

        if (await IsRateLimitedAsync("signout", new AuthRateLimitRule(BuildRateLimitKey("signout", "ip", GetRequesterIp()), 60, TimeSpan.FromMinutes(5))))
        {
            return false;
        }

        if (!AreSafe(refreshToken))
        {
            _logger.LogWarning("Auth signout rejected due to unsafe token input. RequesterIp={RequesterIp}", GetRequesterIp());
            return false;
        }

        var success = await _authService.InvalidateRefreshTokenAsync(refreshToken);
        _logger.LogInformation("Auth signout {Outcome}. RequesterIp={RequesterIp}", success ? "succeeded" : "failed", GetRequesterIp());
        return success;
    }

    public async Task<bool> RequestAccountDeletionAsync(ClaimsPrincipal claimsPrincipal)
    {
        var userId = claimsPrincipal.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Auth request-delete-account rejected because authenticated user identifier was missing.");
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            _logger.LogWarning("Auth request-delete-account failed because user or email was not found. UserId={UserId}", userId);
            return false;
        }

        if (await IsRateLimitedAsync("request-delete-account", new AuthRateLimitRule(BuildRateLimitKey("request-delete-account", "ip", GetRequesterIp()), 1, TimeSpan.FromMinutes(3))))
        {
            return false;
        }

        var code = RandomNumberGenerator.GetInt32(10000, 100000).ToString();
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"delete-account-code:{userId}", code, TimeSpan.FromMinutes(10));

        var retroEmail = AuthRetroEmailBuilder.Build(user.Email, code, "ACCOUNT_DELETION", "SYSTEM - TERMINATION LOG");
        await _publishEndpoint.Publish(new AuthEmailRequested(
            user.Email,
            "SECURE_SYSTEM_MESSAGE: ACCOUNT_DELETION_CODE",
            retroEmail));

        await _context.SaveChangesAsync();

        _logger.LogInformation("Auth request-delete-account accepted and email queued. UserId={UserId}", userId);
        return true;
    }

    public async Task<bool> DeleteAccountAsync(string code, ClaimsPrincipal claimsPrincipal)
    {
        var userId = claimsPrincipal.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Auth delete-account rejected because authenticated user identifier was missing.");
            return false;
        }

        code = AuthInputGuard.Normalize(code);
        if (!AreSafe(code))
        {
            _logger.LogWarning("Auth delete-account rejected due to unsafe code input. UserId={UserId}", userId);
            throw new GraphQLException("Invalid verification code.");
        }

        if (await IsRateLimitedAsync("delete-account", new AuthRateLimitRule(BuildRateLimitKey("delete-account", "ip", GetRequesterIp()), 10, TimeSpan.FromMinutes(10))))
        {
            throw new GraphQLException("Too many requests. Please try again later.");
        }

        var db = _redis.GetDatabase();
        var storedCode = await db.StringGetAsync($"delete-account-code:{userId}");

        if (!storedCode.HasValue || storedCode.ToString() != code)
        {
            _logger.LogWarning("Auth delete-account failed because verification code was missing or invalid. UserId={UserId}", userId);
            throw new GraphQLException("Invalid or expired verification code.");
        }

        var success = await _authService.DeleteAccountAsync(userId);
        if (success)
        {
            await db.KeyDeleteAsync($"delete-account-code:{userId}");
        }

        _logger.LogInformation("Auth delete-account {Outcome}. UserId={UserId}", success ? "succeeded" : "failed", userId);
        
        if (!success)
        {
            throw new GraphQLException("Failed to delete account. Please try again later.");
        }

        return success;
    }

    public async Task<bool> VerifyEmailAsync(string email, string code)
    {
        email = AuthInputGuard.NormalizeEmail(email);
        code = AuthInputGuard.Normalize(code);

        if (!AreSafe(email, code))
        {
            _logger.LogWarning("Auth verify-email rejected due to unsafe input. Email={Email} RequesterIp={RequesterIp}", email, GetRequesterIp());
            return false;
        }

        if (await IsRateLimitedAsync(
                "verify-email",
                new AuthRateLimitRule(BuildRateLimitKey("verify-email", "ip", GetRequesterIp()), 40, TimeSpan.FromMinutes(10)),
                new AuthRateLimitRule(BuildRateLimitKey("verify-email", "email-ip", $"{email}:{GetRequesterIp()}"), 12, TimeSpan.FromMinutes(10))))
        {
            return false;
        }

        var pending = await _context.PendingRegistrations
            .FirstOrDefaultAsync(p => p.Email == email && p.ExpirationTime > DateTime.UtcNow);

        if (pending == null)
        {
            _logger.LogInformation("Auth verify-email failed because no active pending registration was found. Email={Email}", email);
            return false;
        }

        if (!BCrypt.Net.BCrypt.Verify(code, pending.VerificationCode))
        {
            _logger.LogInformation("Auth verify-email failed because the verification code did not match. Email={Email}", email);
            return false;
        }

        var user = new ApplicationUser
        {
            UserName = pending.Email,
            Email = pending.Email,
            Name = pending.Name,
            EmailConfirmed = true,
            PasswordHash = pending.PasswordHash
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Auth verify-email failed because user creation did not succeed. Email={Email} Errors={Errors}", email, string.Join("; ", result.Errors.Select(error => error.Description)));
            return false;
        }

        _context.PendingRegistrations.Remove(pending);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Auth verify-email succeeded. UserId={UserId} Email={Email}", user.Id, email);
        return true;
    }

    public async Task<bool> ResendVerificationAsync(string email)
    {
        email = AuthInputGuard.NormalizeEmail(email);
        if (!AreSafe(email))
        {
            _logger.LogWarning("Auth resend-verification rejected due to unsafe input. Email={Email} RequesterIp={RequesterIp}", email, GetRequesterIp());
            return false;
        }

        if (await IsRateLimitedAsync("resend-verification", new AuthRateLimitRule(BuildRateLimitKey("resend-verification", "ip", GetRequesterIp()), 20, TimeSpan.FromMinutes(10))))
        {
            return false;
        }

        var pending = await _context.PendingRegistrations
            .FirstOrDefaultAsync(p => p.Email == email && p.ExpirationTime > DateTime.UtcNow);

        if (pending == null)
        {
            _logger.LogInformation("Auth resend-verification failed because no active pending registration was found. Email={Email}", email);
            return false;
        }

        if (pending.LastResentAt.HasValue && pending.LastResentAt.Value.AddMinutes(2) > DateTime.UtcNow)
        {
            _logger.LogInformation("Auth resend-verification rejected due to resend cooldown. Email={Email}", email);
            return false;
        }

        var now = DateTime.UtcNow;
        var code = RandomNumberGenerator.GetInt32(10000, 100000).ToString();
        var retroEmail = AuthRetroEmailBuilder.Build(email, code, "EMAIL_VERIFICATION_RESEND", "SYSTEM - DISPATCH LOG");

        await _publishEndpoint.Publish(new AuthEmailRequested(
            email,
            "SECURE_SYSTEM_MESSAGE: RESEND_VERIFICATION_CODE",
            retroEmail));

        pending.VerificationCode = BCrypt.Net.BCrypt.HashPassword(code);
        pending.LastResentAt = now;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Auth resend-verification succeeded and email was queued. Email={Email}", email);
        return true;
    }

    private bool AreSafe(params string[] values)
        => values.All(AuthInputGuard.IsSafe);

    private async Task<bool> IsRateLimitedAsync(string operation, params AuthRateLimitRule[] rules)
    {
        foreach (var rule in rules)
        {
            if (!await AuthRateLimitGuard.IsRateLimitedAsync(_redis, rule.Key, rule.MaxRequests, rule.Window))
            {
                continue;
            }

            _logger.LogWarning("Auth {Operation} rate limited. RateLimitKey={RateLimitKey} RequesterIp={RequesterIp}", operation, rule.Key, GetRequesterIp());
            return true;
        }

        return false;
    }

    private string GetRequesterIp()
        => _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";

    private static string BuildRateLimitKey(string operation, string scope, string identifier)
        => $"ratelimit:auth:{operation}:{scope}:{AuthRateLimitGuard.NormalizePart(identifier)}";

    private readonly record struct AuthRateLimitRule(string Key, int MaxRequests, TimeSpan Window);
}
