namespace Page.Ui.Application.Auth.Contracts;

public record AuthEmailRequested
{
    public AuthEmailRequested() { }

    public AuthEmailRequested(string recipientEmail, string subject, string htmlBody)
    {
        RecipientEmail = recipientEmail;
        Subject = subject;
        HtmlBody = htmlBody;
    }

    public string RecipientEmail { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
}
