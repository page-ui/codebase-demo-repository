using Page.Ui.Application.Chat.Contracts;

namespace Page.Ui.Presentation.Chat.Time;

public sealed class ChatClientTimeConverter
{
    private static readonly string[] FallbackTimeZoneIds =
    [
        "Africa/Cairo",
        "Egypt Standard Time"
    ];

    private readonly ILogger<ChatClientTimeConverter> _logger;
    private readonly TimeZoneInfo _timeZone;

    public ChatClientTimeConverter(IConfiguration configuration, ILogger<ChatClientTimeConverter> logger)
    {
        _logger = logger;
        _timeZone = ResolveTimeZone(configuration["Chat:DisplayTimeZone"] ?? configuration["Chat__DisplayTimeZone"]);
    }

    public DateTimeOffset Convert(DateTimeOffset value)
    {
        return TimeZoneInfo.ConvertTime(value, _timeZone);
    }

    public DateTimeOffset? Convert(DateTimeOffset? value)
    {
        return value.HasValue ? Convert(value.Value) : null;
    }

    public ChatMessageCreated Convert(ChatMessageCreated message)
    {
        return new ChatMessageCreated(
            message.Id,
            message.ChatId,
            message.ChatKey,
            message.MessageKey,
            message.SenderId,
            message.Title,
            message.Content,
            message.Type,
            Convert(message.CreatedAt),
            message.Status,
            message.AttachmentUrl,
            message.ServerGeneratedId,
            message.ReplyToMessageId,
            message.IsQuestion);
    }

    private TimeZoneInfo ResolveTimeZone(string? configuredTimeZoneId)
    {
        foreach (var candidate in EnumerateCandidates(configuredTimeZoneId))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException ex)
            {
                _logger.LogWarning(ex, "Invalid chat display timezone configuration for {TimeZoneId}", candidate);
            }
        }

        _logger.LogWarning("Falling back to UTC for chat display timestamps because no configured timezone could be resolved.");
        return TimeZoneInfo.Utc;
    }

    private static IEnumerable<string> EnumerateCandidates(string? configuredTimeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(configuredTimeZoneId))
        {
            yield return configuredTimeZoneId.Trim();
        }

        foreach (var fallback in FallbackTimeZoneIds)
        {
            yield return fallback;
        }
    }
}
