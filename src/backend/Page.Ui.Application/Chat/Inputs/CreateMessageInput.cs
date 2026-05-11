using Page.Ui.Domain.Chat.Enums;

namespace Page.Ui.Application.Chat.Inputs;

public record CreateMessageInput(
    string ChatKey,
    string Content,
    string? ReplyToKey,
    string? AttachmentUrl,
    string? ClientRequestId = null,
    MessageType? Type = null,
    bool IsQuestion = false
);
