using System.Linq.Expressions;
using Page.Ui.Application.Chat.Contracts;
using Page.Ui.Application.Chat.Payloads;
using Page.Ui.Domain.Chat;
using Page.Ui.Domain.Chat.Entities;

namespace Page.Ui.Presentation.Chat.GraphQl.Views;

public static class ChatGraphQlMapper
{
    public static Expression<Func<Page.Ui.Domain.Chat.Entities.Chat, ChatView>> ProjectChat()
    {
        return chat => new ChatView
        {
            ChatKey = chat.ChatKey,
            Name = chat.Name,
            ModelId = chat.ModelId,
            CreatedAt = chat.CreatedAt,
            UpdatedAt = chat.UpdatedAt
        };
    }

    public static Expression<Func<Message, MessageView>> ProjectMessage(string currentUserId)
    {
        return message => new MessageView
        {
            MessageKey = message.MessageKey,
            ChatKey = message.Chat.ChatKey,
            Title = message.Title,
            Content = message.Content,
            IsQuestion = message.IsQuestion,
            Type = message.Type,
            Status = message.Status,
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt,
            ReplyToKey = message.ReplyTo == null ? null : message.ReplyTo.MessageKey,
            AttachmentUrl = message.AttachmentUrl,
            SenderType = message.SenderId == ChatConstants.AiBotUserId
                ? "assistant"
                : message.SenderId == currentUserId
                    ? "user"
                    : message.SenderId == ""
                        ? "unknown"
                        : "user"
        };
    }

    public static ChatView ToView(Page.Ui.Domain.Chat.Entities.Chat chat)
    {
        return new ChatView
        {
            ChatKey = chat.ChatKey,
            Name = chat.Name,
            ModelId = chat.ModelId,
            CreatedAt = chat.CreatedAt,
            UpdatedAt = chat.UpdatedAt
        };
    }

    public static MessageView ToView(Message message, string currentUserId)
    {
        return ToView(
            message,
            message.Chat.ChatKey,
            message.ReplyTo?.MessageKey,
            currentUserId);
    }

    public static MessageView ToView(Message message, string chatKey, string? replyToKey, string currentUserId)
    {
        return new MessageView
        {
            MessageKey = message.MessageKey,
            ChatKey = chatKey,
            Title = message.Title,
            Content = message.Content,
            IsQuestion = message.IsQuestion,
            Type = message.Type,
            Status = message.Status,
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt,
            ReplyToKey = replyToKey,
            AttachmentUrl = message.AttachmentUrl,
            SenderType = ResolveSenderType(message.SenderId, currentUserId)
        };
    }

    public static MessageView ToPublicEventView(ChatMessageCreated message, string? replyToKey)
    {
        return new MessageView
        {
            MessageKey = message.MessageKey,
            ChatKey = message.ChatKey,
            Title = message.Title,
            Content = message.Content,
            IsQuestion = message.IsQuestion,
            Type = message.Type,
            Status = message.Status,
            CreatedAt = message.CreatedAt,
            UpdatedAt = null,
            ReplyToKey = replyToKey,
            AttachmentUrl = message.AttachmentUrl,
            SenderType = ResolveSenderType(message.SenderId, null)
        };
    }

    public static CreateChatPayloadView ToView(CreateChatPayload payload, string currentUserId)
    {
        return new CreateChatPayloadView(
            ToView(payload.Chat),
            payload.InitialMessage is null
                ? null
                : ToView(payload.InitialMessage, payload.Chat.ChatKey, null, currentUserId));
    }

    private static string ResolveSenderType(string senderId, string? currentUserId)
    {
        if (senderId == ChatConstants.AiBotUserId)
        {
            return "assistant";
        }

        if (!string.IsNullOrWhiteSpace(currentUserId) && senderId == currentUserId)
        {
            return "user";
        }

        return string.IsNullOrWhiteSpace(senderId) ? "unknown" : "user";
    }
}
