using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Page.Ui.Infrastructure.Auth.Services;
using Page.Ui.Domain.Auth.Entities;
using Page.Ui.Backend.Tests.TestSupport;

namespace Page.Ui.Backend.Tests.Auth;

public class AuthServiceImplTests
{
    [Fact]
    public async Task GenerateRefreshTokenAsync_StoresHashedToken_NotRawToken()
    {
        using var context = TestDbFactory.CreateContext();

        context.Users.Add(new ApplicationUser
        {
            Id = "user-1",
            UserName = "user@example.com",
            NormalizedUserName = "USER@EXAMPLE.COM",
            Email = "user@example.com",
            NormalizedEmail = "USER@EXAMPLE.COM",
            EmailConfirmed = true,
            Name = "User",
            SecurityStamp = Guid.NewGuid().ToString("N")
        });
        await context.SaveChangesAsync();

        using var rsa = new TestRsaKeyService();
        var authService = new AuthService(
            context,
            TestDbFactory.CreateUserManager(context),
            BuildConfiguration(),
            rsa);


        var raw = await authService.GenerateRefreshTokenAsync("user-1", "127.0.0.1");

        var stored = Assert.Single(context.RefreshTokens);
        Assert.NotEqual(raw, stored.HashedToken);
        Assert.False(string.IsNullOrWhiteSpace(stored.HashedToken));
    }

    [Fact]
    public async Task RefreshAccessTokenAsync_InvalidatesAllUserTokens_WhenStoredTokenAlreadyUsed()
    {
        using var context = TestDbFactory.CreateContext();

        context.Users.Add(new ApplicationUser
        {
            Id = "user-1",
            UserName = "user@example.com",
            NormalizedUserName = "USER@EXAMPLE.COM",
            Email = "user@example.com",
            NormalizedEmail = "USER@EXAMPLE.COM",
            EmailConfirmed = true,
            Name = "User",
            SecurityStamp = Guid.NewGuid().ToString("N")
        });
        await context.SaveChangesAsync();

        using var rsa = new TestRsaKeyService();
        var authService = new AuthService(
            context,
            TestDbFactory.CreateUserManager(context),
            BuildConfiguration(),
            rsa);

        var raw = await authService.GenerateRefreshTokenAsync("user-1", "127.0.0.1");
        var secondRaw = await authService.GenerateRefreshTokenAsync("user-1", "127.0.0.1");
        var firstHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        var first = context.RefreshTokens.Single(x => x.HashedToken == firstHash);
        first.Used = true;
        await context.SaveChangesAsync();

        var result = await authService.RefreshAccessTokenAsync(raw, "127.0.0.1");

        Assert.False(result.Success);
        Assert.All(context.RefreshTokens, token => Assert.True(token.Invalidated || token.Used));
        Assert.NotEqual(raw, secondRaw);
    }

    [Fact]
    public async Task VerifyPasswordResetCodeAsync_MarksCodeVerified_AndReturnsTempToken()
    {
        using var context = TestDbFactory.CreateContext();

        context.PasswordResetCodes.Add(new PasswordResetCode
        {
            Email = "user@example.com",
            Code = BCrypt.Net.BCrypt.HashPassword("12345"),
            ExpirationTime = DateTime.UtcNow.AddMinutes(15)
        });
        await context.SaveChangesAsync();

        using var rsa = new TestRsaKeyService();
        var authService = new AuthService(
            context,
            TestDbFactory.CreateUserManager(context),
            BuildConfiguration(),
            rsa);

        var token = await authService.VerifyPasswordResetCodeAsync("user@example.com", "12345");

        Assert.False(string.IsNullOrWhiteSpace(token));
        var resetCode = Assert.Single(context.PasswordResetCodes);
        Assert.True(resetCode.IsVerified);
        Assert.Equal(token, resetCode.TempToken);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT:Issuer"] = "AuthService",
                ["JWT:Audience"] = "AuthService",
                ["JWT:AccessTokenExpirationMinutes"] = "120",
                ["JWT:RefreshTokenExpirationDays"] = "7"
            })
            .Build();
    }
}
