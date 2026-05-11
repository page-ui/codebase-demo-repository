using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Moq;
using Page.Ui.Application.Chat.Configuration;
using Page.Ui.Application.Common.Interfaces;
using Page.Ui.Infrastructure.Auth.Services;

namespace Page.Ui.Backend.Tests.Auth;

public class InternalServiceJwtProviderTests
{
    [Fact]
    public void CreateAiApiToken_UsesFifteenMinuteMinimumExpiration()
    {
        var rsa = RSA.Create(2048);
        var rsaKeyService = new Mock<IRsaKeyService>();
        rsaKeyService.Setup(x => x.GetPrivateKey()).Returns(rsa);
        var provider = new InternalServiceJwtProvider(
            rsaKeyService.Object,
            Options.Create(new InternalServiceJwtOptions
            {
                Issuer = "issuer",
                Audience = "audience",
                ExpirationMinutes = 1
            }));

        var before = DateTime.UtcNow;
        var token = provider.CreateAiApiToken(Guid.NewGuid(), Guid.NewGuid(), "user-1");
        var after = DateTime.UtcNow;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.True(jwt.ValidTo >= before.AddMinutes(15).AddSeconds(-5));
        Assert.True(jwt.ValidTo <= after.AddMinutes(15).AddSeconds(5));
    }
}
