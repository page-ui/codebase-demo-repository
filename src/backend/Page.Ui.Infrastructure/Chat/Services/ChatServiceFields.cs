namespace Page.Ui.Infrastructure.Chat.Services;

internal static class ChatServiceFields
{
    public const int MaxChatNameLength = 100;
    public const int MaxMessageTitleLength = 160;
    public const int MaxSystemPromptLength = 4000;
    public const int MaxMessageContentLength = 10_000;
    public const int MaxAttachmentUrlLength = 4000;
    public const int MaxClientRequestIdLength = 128;

    public static string SanitizeRequiredField(string? value, int maxLength, string fieldName)
    {
        var sanitized = SanitizeOptionalField(value, maxLength, fieldName);
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            throw new InvalidOperationException($"{fieldName} is required.");
        }

        return sanitized;
    }

    public static string? SanitizeOptionalField(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Trim();

        if (normalized.Any(c => char.IsControl(c) && c != '\n'))
        {
            throw new InvalidOperationException($"{fieldName} contains control characters.");
        }

        if (normalized.Length > maxLength)
        {
            throw new InvalidOperationException($"{fieldName} exceeds maximum length of {maxLength}.");
        }

        return normalized;
    }
}
