using System.Security.Cryptography;
using System.Text;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Page.Ui.Application.Chat.Contracts;
using Page.Ui.Application.Common.Interfaces;
using Page.Ui.Domain.Chat;
using Page.Ui.Domain.Chat.Entities;
using Page.Ui.Domain.Chat.Enums;
using Page.Ui.Domain.Common;

namespace Page.Ui.Presentation.Chat.Consumers;

public class AiResponseMessageGeneratedConsumer : IConsumer<AiResponseMessageGenerated>
{
    private readonly IApplicationDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<AiResponseMessageGeneratedConsumer> _logger;

    public AiResponseMessageGeneratedConsumer(
        IApplicationDbContext context,
        IPublishEndpoint publishEndpoint,
        ILogger<AiResponseMessageGeneratedConsumer> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AiResponseMessageGenerated> context)
    {
        var msg = context.Message;
        var isThinkingUpdate = msg.Type == MessageType.Thinking;
        if (isThinkingUpdate)
        {
            _logger.LogInformation(
                "Rebroadcasting transient AI thinking update. ChatId={ChatId} ResponseType={ResponseType}",
                msg.ChatId,
                msg.Type);
        }
        else
        {
            _logger.LogInformation(
                "Persisting AI response message. ChatId={ChatId} ResponseType={ResponseType}",
                msg.ChatId,
                msg.Type);
        }

        var chat = await _context.Chats
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == msg.ChatId, context.CancellationToken);

        var chatKey = chat?.ChatKey ?? OpaqueKey.FromGuid(msg.ChatId);
        var messageId = isThinkingUpdate
            ? BuildThinkingMessageId(msg)
            : Guid.NewGuid();

        var message = new Message
        {
            Id = messageId,
            ChatId = msg.ChatId,
            SenderId = ChatConstants.AiBotUserId,
            Title = msg.Title,
            Content = msg.Content,
            IsQuestion = msg.IsQuestion,
            Type = msg.Type,
            ReplyToId = msg.ReplyToMessageId,
            ServerGeneratedId = UlidGenerator.NewUlid(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        message.MessageKey = isThinkingUpdate
            ? BuildThinkingMessageKey(msg)
            : OpaqueKey.FromGuid(message.Id);

        try
        {
            await _publishEndpoint.Publish(new ChatMessageCreated(
                message.Id,
                message.ChatId,
                chatKey,
                message.MessageKey,
                message.SenderId,
                message.Title,
                message.Content,
                message.Type,
                message.CreatedAt,
                message.Status,
                null,
                message.ServerGeneratedId,
                message.ReplyToId,
                message.IsQuestion
            ), context.CancellationToken);

            if (!isThinkingUpdate)
            {
                _context.Messages.Add(message);
                await _context.SaveChangesAsync(context.CancellationToken);

                _logger.LogInformation(
                    "AI response persisted and rebroadcast queued. ChatId={ChatId} MessageId={MessageId}",
                    message.ChatId,
                    message.Id);
            }
            else
            {
                _logger.LogInformation(
                    "AI thinking update rebroadcast queued and skipped persistence. ChatId={ChatId}",
                    message.ChatId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "AI response persistence failed. ChatId={ChatId}",
                msg.ChatId);
            throw;
        }
    }

    private static Guid BuildThinkingMessageId(AiResponseMessageGenerated message)
    {
        var source = BuildThinkingIdentitySource(message);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static string BuildThinkingMessageKey(AiResponseMessageGenerated message)
    {
        return OpaqueKey.FromString(BuildThinkingIdentitySource(message));
    }

    private static string BuildThinkingIdentitySource(AiResponseMessageGenerated message)
    {
        var trigger = message.ReplyToMessageId?.ToString("N") ?? "chat";
        return $"thinking:{message.ChatId:N}:{trigger}";
    }
}
