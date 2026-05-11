using HotChocolate.Subscriptions;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Page.Ui.Application.Chat.Contracts;
using Page.Ui.Application.Common.Interfaces;
using Page.Ui.Presentation.Chat.GraphQl.Views;
using Page.Ui.Presentation.Chat.Hubs;
using Page.Ui.Presentation.Chat.Time;

namespace Page.Ui.Presentation.Chat.Consumers;

public class ChatMessageCreatedConsumer : IConsumer<ChatMessageCreated>
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ITopicEventSender _eventSender;
    private readonly IApplicationDbContext _dbContext;
    private readonly ChatClientTimeConverter _timeConverter;
    private readonly ILogger<ChatMessageCreatedConsumer> _logger;

    public ChatMessageCreatedConsumer(
        IHubContext<ChatHub> hubContext,
        ITopicEventSender eventSender,
        IApplicationDbContext dbContext,
        ChatClientTimeConverter timeConverter,
        ILogger<ChatMessageCreatedConsumer> logger)
    {
        _hubContext = hubContext;
        _eventSender = eventSender;
        _dbContext = dbContext;
        _timeConverter = timeConverter;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ChatMessageCreated> context)
    {
        var msg = context.Message;
        var clientMessage = _timeConverter.Convert(msg);
        var chatKey = msg.ChatKey;

        try
        {
            await _hubContext.Clients.Group(chatKey)
                .SendAsync("ReceiveMessage", clientMessage);

            var topicName = $"OnMessageCreated_{chatKey}";
            var replyToKey = await ResolveReplyToKeyAsync(msg, context.CancellationToken);
            await _eventSender.SendAsync(topicName, ChatGraphQlMapper.ToPublicEventView(msg, replyToKey));

            _logger.LogDebug(
                "Chat message fanout completed. MessageId={MessageId} ChatKey={ChatKey} TopicName={TopicName}",
                msg.Id,
                chatKey,
                topicName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Chat message fanout failed. MessageId={MessageId} ChatKey={ChatKey}",
                msg.Id,
                chatKey);
        }
    }

    private async Task<string?> ResolveReplyToKeyAsync(ChatMessageCreated message, CancellationToken cancellationToken)
    {
        if (!message.ReplyToMessageId.HasValue)
        {
            return null;
        }

        return await _dbContext.Messages
            .AsNoTracking()
            .Where(m => m.Id == message.ReplyToMessageId.Value && m.ChatId == message.ChatId)
            .Select(m => m.MessageKey)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
