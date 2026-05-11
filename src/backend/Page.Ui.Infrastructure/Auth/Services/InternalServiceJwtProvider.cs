using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Page.Ui.Application.Common.Interfaces;
using Page.Ui.Application.Chat.Configuration;

namespace Page.Ui.Infrastructure.Auth.Services;

public sealed class InternalServiceJwtProvider : IInternalServiceJwtProvider
{
    private const int MinimumAiTokenExpirationMinutes = 15;

    private readonly IRsaKeyService _rsaKeyService;
    private readonly InternalServiceJwtOptions _options;

    public InternalServiceJwtProvider(IRsaKeyService rsaKeyService, IOptions<InternalServiceJwtOptions> options)
    {
        _rsaKeyService = rsaKeyService;
        _options = options.Value;
    }

    public string CreateAiApiToken(Guid chatId, Guid messageId, string userId)
    {
        using var rsa = _rsaKeyService.GetPrivateKey();
        var credentials = new SigningCredentials(new RsaSecurityKey(rsa.ExportParameters(true)), SecurityAlgorithms.RsaSha256);
        var now = DateTime.UtcNow;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "worker-ai"),
            new Claim("scope", "ai-api.invoke"),
            new Claim("chat_id", chatId.ToString()),
            new Claim("message_id", messageId.ToString()),
            new Claim("user_id", userId)
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(Math.Max(MinimumAiTokenExpirationMinutes, _options.ExpirationMinutes)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
