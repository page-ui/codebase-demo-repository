namespace Page.Ui.Application.Chat.Configuration;

public sealed class InternalServiceJwtOptions
{
    public string Issuer { get; set; } = "Page.Ui.Worker.Ai";
    public string Audience { get; set; } = "AiModelApi";
    public int ExpirationMinutes { get; set; } = 5;
}
