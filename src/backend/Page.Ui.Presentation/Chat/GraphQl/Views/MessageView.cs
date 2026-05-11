using Page.Ui.Domain.Chat.Enums;

namespace Page.Ui.Presentation.Chat.GraphQl.Views;

public sealed record MessageView
{
    public string MessageKey { get; init; } = string.Empty;
    public string ChatKey { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public bool IsQuestion { get; init; }
    public MessageType Type { get; init; }
    public MessageStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? ReplyToKey { get; init; }
    public string? AttachmentUrl { get; init; }
    public string SenderType { get; init; } = string.Empty;
}
