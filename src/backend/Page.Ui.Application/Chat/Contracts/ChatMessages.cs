using Page.Ui.Domain.Chat.Enums;

namespace Page.Ui.Application.Chat.Contracts;

public record ChatMessageCreated
{
    public ChatMessageCreated() { }
    public ChatMessageCreated(Guid id, Guid chatId, string chatKey, string messageKey, string senderId, string title, string content, MessageType type, DateTimeOffset createdAt, MessageStatus status, string? attachmentUrl, string? serverGeneratedId, Guid? replyToMessageId = null, bool isQuestion = false)
    {
        Id = id;
        ChatId = chatId;
        ChatKey = chatKey;
        MessageKey = messageKey;
        SenderId = senderId;
        Title = title;
        Content = content;
        Type = type;
        CreatedAt = createdAt;
        Status = status;
        AttachmentUrl = attachmentUrl;
        ServerGeneratedId = serverGeneratedId;
        ReplyToMessageId = replyToMessageId;
        IsQuestion = isQuestion;
    }

    public Guid Id { get; init; }
    public Guid ChatId { get; init; }
    public string ChatKey { get; init; } = string.Empty;
    public string MessageKey { get; init; } = string.Empty;
    public string SenderId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public bool IsQuestion { get; init; }
    public MessageType Type { get; init; } = MessageType.Text;
    public DateTimeOffset CreatedAt { get; init; }
    public MessageStatus Status { get; init; }
    public string? AttachmentUrl { get; init; }
    public string? ServerGeneratedId { get; init; }
    public Guid? ReplyToMessageId { get; init; }
}

public record ChatMessageUpdated
{
    public ChatMessageUpdated() { }
    public ChatMessageUpdated(Guid messageId, Guid chatId, string newContent, DateTimeOffset updatedAt, string? aiSummary)
    {
        MessageId = messageId;
        ChatId = chatId;
        NewContent = newContent;
        UpdatedAt = updatedAt;
        AiSummary = aiSummary;
    }

    public Guid MessageId { get; init; }
    public Guid ChatId { get; init; }
    public string NewContent { get; init; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; init; }
    public string? AiSummary { get; init; }
}

public record AiResponseMessageGenerated
{
    public AiResponseMessageGenerated() { }
    public AiResponseMessageGenerated(Guid chatId, string title, string content, MessageType type = MessageType.AiRun, Guid? replyToMessageId = null, Guid? runId = null, Guid? versionId = null, bool isQuestion = false)
    {
        ChatId = chatId;
        Title = title;
        Content = content;
        Type = type;
        ReplyToMessageId = replyToMessageId;
        RunId = runId;
        VersionId = versionId;
        IsQuestion = isQuestion;
    }

    public Guid ChatId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public bool IsQuestion { get; init; }
    public MessageType Type { get; init; } = MessageType.AiRun;
    public Guid? ReplyToMessageId { get; init; }
    public Guid? RunId { get; init; }
    public Guid? VersionId { get; init; }
}
