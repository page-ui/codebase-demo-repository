using HotChocolate.Subscriptions;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Page.Ui.Application.Auth.Contracts;
using Page.Ui.Application.Auth.Interfaces;
using Page.Ui.Application.Chat.Contracts;
using Page.Ui.Domain.Chat.Enums;
using Page.Ui.Infrastructure.Auth.Persistence;
using Page.Ui.Presentation.Auth.Consumers;
using Page.Ui.Presentation.Chat.Consumers;
using Page.Ui.Presentation.Chat.GraphQl.Views;
using Page.Ui.Presentation.Chat.Hubs;
using Page.Ui.Presentation.Chat.Time;
using Page.Ui.Worker.Ai.Consumers;
using Page.Ui.Worker.Ai.Models;
using Page.Ui.Worker.Ai.Services;
using Page.Ui.Backend.Tests.TestSupport;
using StackExchange.Redis;

namespace Page.Ui.Backend.Tests.Events;

public class EventConsumerTests
{
    [Fact]
    public async Task AuthEmailRequestedConsumer_Throws_WhenEmailSendFails()
    {
        var email = new Mock<IEmailService>();
        email.Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var consumer = new AuthEmailRequestedConsumer(email.Object, NullLogger<AuthEmailRequestedConsumer>.Instance);
        var context = new Mock<ConsumeContext<AuthEmailRequested>>();
        context.SetupGet(x => x.Message).Returns(new AuthEmailRequested("user@example.com", "subject", "body"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.Consume(context.Object));
    }

    [Fact]
    public async Task AiResponseMessageGeneratedConsumer_PersistsMessage_AndPublishesChatMessageCreated()
    {
        using var db = TestDbFactory.CreateContext();

        var publish = new Mock<IPublishEndpoint>();
        publish.Setup(x => x.Publish(It.IsAny<ChatMessageCreated>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var consumer = new AiResponseMessageGeneratedConsumer(db, publish.Object, NullLogger<AiResponseMessageGeneratedConsumer>.Instance);
        var context = new Mock<ConsumeContext<AiResponseMessageGenerated>>();
        var replyToId = Guid.NewGuid();
        context.SetupGet(x => x.Message).Returns(new AiResponseMessageGenerated(Guid.NewGuid(), "Run title", "/runs/preview.html", MessageType.AiRun, replyToId));
        context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(context.Object);

        var message = Assert.Single(db.Messages);
        Assert.Equal("Run title", message.Title);
        Assert.Equal("/runs/preview.html", message.Content);
        Assert.Equal(MessageType.AiRun, message.Type);
        Assert.Equal(replyToId, message.ReplyToId);
        publish.Verify(x => x.Publish(It.IsAny<ChatMessageCreated>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AiResponseMessageGeneratedConsumer_RebroadcastsThinkingWithoutPersistingOrLoggingPersist()
    {
        using var db = TestDbFactory.CreateContext();

        var publish = new Mock<IPublishEndpoint>();
        publish.Setup(x => x.Publish(It.IsAny<ChatMessageCreated>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var logger = new CapturingLogger<AiResponseMessageGeneratedConsumer>();
        var consumer = new AiResponseMessageGeneratedConsumer(db, publish.Object, logger);
        var context = new Mock<ConsumeContext<AiResponseMessageGenerated>>();
        context.SetupGet(x => x.Message).Returns(new AiResponseMessageGenerated(
            Guid.NewGuid(),
            "Thinking",
            "Reviewing the prompt",
            MessageType.Thinking));
        context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(context.Object);

        Assert.Empty(db.Messages);
        publish.Verify(x => x.Publish(It.Is<ChatMessageCreated>(msg => msg.Type == MessageType.Thinking), It.IsAny<CancellationToken>()), Times.Once);
        Assert.DoesNotContain(logger.Messages, message => message.Contains("Persisting AI response message", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("Rebroadcasting transient AI thinking update", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AiResponseMessageGeneratedConsumer_ReusesThinkingMessageIdentityForSameTrigger()
    {
        using var db = TestDbFactory.CreateContext();

        var publishedMessages = new List<ChatMessageCreated>();
        var publish = new Mock<IPublishEndpoint>();
        publish.Setup(x => x.Publish(It.IsAny<ChatMessageCreated>(), It.IsAny<CancellationToken>()))
            .Callback<ChatMessageCreated, CancellationToken>((message, _) => publishedMessages.Add(message))
            .Returns(Task.CompletedTask);

        var consumer = new AiResponseMessageGeneratedConsumer(db, publish.Object, NullLogger<AiResponseMessageGeneratedConsumer>.Instance);
        var chatId = Guid.NewGuid();
        var triggerMessageId = Guid.NewGuid();

        foreach (var content in new[] { "thinking-one", "thinking-two" })
        {
            var context = new Mock<ConsumeContext<AiResponseMessageGenerated>>();
            context.SetupGet(x => x.Message).Returns(new AiResponseMessageGenerated(
                chatId,
                "Thinking",
                content,
                MessageType.Thinking,
                triggerMessageId));
            context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

            await consumer.Consume(context.Object);
        }

        Assert.Empty(db.Messages);
        Assert.Equal(2, publishedMessages.Count);
        Assert.Equal("thinking-one", publishedMessages[0].Content);
        Assert.Equal("thinking-two", publishedMessages[1].Content);
        Assert.Equal(publishedMessages[0].Id, publishedMessages[1].Id);
        Assert.Equal(publishedMessages[0].MessageKey, publishedMessages[1].MessageKey);
    }

    [Fact]
    public async Task PresentationChatMessageCreatedConsumer_BroadcastsToHub_AndTopic()
    {
        using var db = TestDbFactory.CreateContext();
        var hubProxy = new Mock<IClientProxy>();
        hubProxy.Setup(x => x.SendCoreAsync("ReceiveMessage", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hubClients = new Mock<IHubClients>();
        var chatId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var chatKey = "chat-key";
        hubClients.Setup(x => x.Group(chatKey)).Returns(hubProxy.Object);

        var hubContext = new Mock<IHubContext<ChatHub>>();
        hubContext.SetupGet(x => x.Clients).Returns(hubClients.Object);
        var replyToId = Guid.NewGuid();
        db.Messages.Add(new Page.Ui.Domain.Chat.Entities.Message
        {
            Id = replyToId,
            ChatId = chatId,
            SenderId = "user-1",
            Title = "Room title",
            Content = "question",
            MessageKey = "reply-key",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var eventSender = new Mock<ITopicEventSender>();
        object? sentGraphQlPayload = null;
        eventSender.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?, CancellationToken>((_, payload, _) => sentGraphQlPayload = payload)
            .Returns(ValueTask.CompletedTask);

        var consumer = new Page.Ui.Presentation.Chat.Consumers.ChatMessageCreatedConsumer(
            hubContext.Object,
            eventSender.Object,
            db,
            new ChatClientTimeConverter(
                new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?> { ["Chat:DisplayTimeZone"] = "UTC" })
                    .Build(),
                NullLogger<ChatClientTimeConverter>.Instance),
            NullLogger<Page.Ui.Presentation.Chat.Consumers.ChatMessageCreatedConsumer>.Instance);

        var message = new ChatMessageCreated(Guid.NewGuid(), chatId, chatKey, "msg-key", "user-1", "Room title", "hello", MessageType.UserMessage, DateTimeOffset.UtcNow, MessageStatus.Sent, null, "srv", replyToId);
        var context = new Mock<ConsumeContext<ChatMessageCreated>>();
        context.SetupGet(x => x.Message).Returns(message);

        await consumer.Consume(context.Object);

        hubProxy.Verify(x => x.SendCoreAsync("ReceiveMessage", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
        eventSender.Verify(x => x.SendAsync(It.Is<string>(s => s.StartsWith("OnMessageCreated_")), It.IsAny<object?>(), It.IsAny<CancellationToken>()), Times.Once);
        var publicPayload = Assert.IsType<MessageView>(sentGraphQlPayload);
        Assert.Equal("msg-key", publicPayload.MessageKey);
        Assert.Equal(chatKey, publicPayload.ChatKey);
        Assert.Equal("reply-key", publicPayload.ReplyToKey);
        Assert.Equal("user", publicPayload.SenderType);
    }

    [Fact]
    public async Task WorkerChatMessageCreatedConsumer_PublishesModelErrorFallback_WhenAiModelApiFails()
    {
        using var appDb = TestDbFactory.CreateContext();
        appDb.Users.Add(new Page.Ui.Domain.Auth.Entities.ApplicationUser
        {
            Id = "user-1",
            UserName = "user-1@example.com",
            NormalizedUserName = "USER-1@EXAMPLE.COM",
            Email = "user-1@example.com",
            NormalizedEmail = "USER-1@EXAMPLE.COM",
            EmailConfirmed = true,
            Name = "User One",
            SecurityStamp = Guid.NewGuid().ToString("N")
        });
        var chatId = Guid.NewGuid();
        appDb.Chats.Add(new Page.Ui.Domain.Chat.Entities.Chat
        {
            Id = chatId,
            OwnerUserId = "user-1",
            Name = "Room title",
            ChatKey = "chat-key",
            ModelId = "assistant-default",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        var triggerMessageId = Guid.NewGuid();
        appDb.Messages.Add(new Page.Ui.Domain.Chat.Entities.Message
        {
            Id = triggerMessageId,
            ChatId = chatId,
            SenderId = "user-1",
            Title = "Room title",
            Content = "hello",
            Type = MessageType.UserMessage,
            Status = MessageStatus.Sent,
            MessageKey = "trigger-msg-key",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await appDb.SaveChangesAsync();

        var (redis, _) = TestRedisFactory.Create();

        var contextLoader = new Mock<IAiContextLoader>();
        contextLoader.Setup(x => x.LoadAsync(It.IsAny<ChatMessageCreated>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AiChatContext
            {
                Chat = appDb.Chats.Single(),
                TriggerMessage = appDb.Messages.Single(),
                History = appDb.Messages.ToList()
            });

        var modelClient = new Mock<IAiModelClient>();
        modelClient.Setup(x => x.GenerateAsync(It.IsAny<AiChatContext>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AiModelDispatchResult.Failed("The AI model API could not be reached right now."));

        var runStorage = new Mock<IAiRunStorageService>();
        runStorage.Setup(x => x.StoreAsync(It.IsAny<AiChatContext>(), It.IsAny<AiModelResult>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredAiRun
            {
                Run = new Page.Ui.Domain.Chat.Entities.AiRun
                {
                    Id = Guid.NewGuid(),
                    VersionId = Guid.NewGuid(),
                    ChatId = chatId,
                    OwnerUserId = "user-1",
                    TriggerMessageId = triggerMessageId,
                    ModelId = "assistant-default",
                    Title = "Room title",
                    ManifestObjectKey = "manifest"
                },
                Files = new[]
                {
                    new Page.Ui.Domain.Chat.Entities.AiRunFile
                    {
                        ObjectKey = "users/storage/chats/chat-key/versions/version/source/001-index.html",
                        StoredFileName = "001-index.html",
                        ContentType = "text/html"
                    }
                },
                UserStorageKey = "storage-key"
            });

        var consumer = new Page.Ui.Worker.Ai.Consumers.ChatMessageCreatedConsumer(
            NullLogger<Page.Ui.Worker.Ai.Consumers.ChatMessageCreatedConsumer>.Instance,
            redis.Object,
            contextLoader.Object,
            modelClient.Object,
            runStorage.Object,
            Mock.Of<IThinkingMessageProvider>());

        var context = new Mock<ConsumeContext<ChatMessageCreated>>();
        context.SetupGet(x => x.Message).Returns(new ChatMessageCreated(
            triggerMessageId,
            chatId,
            "chat-key",
            "msg-key",
            "user-1",
            "Room title",
            "hello",
            MessageType.UserMessage,
            DateTimeOffset.UtcNow,
            MessageStatus.Sent,
            null,
            "srv"));
        context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);
        
        var publishedMessages = new List<object>();
        context.Setup(x => x.Publish(It.IsAny<AiResponseMessageGenerated>(), It.IsAny<CancellationToken>()))
            .Callback<AiResponseMessageGenerated, CancellationToken>((msg, _) => publishedMessages.Add(msg))
            .Returns(Task.CompletedTask);
        context.Setup(x => x.Publish(It.IsAny<TriggerAiRunRender>(), It.IsAny<CancellationToken>()))
            .Callback<TriggerAiRunRender, CancellationToken>((msg, _) => publishedMessages.Add(msg))
            .Returns(Task.CompletedTask);

        await consumer.Consume(context.Object);

        Assert.Contains(publishedMessages, m => m is AiResponseMessageGenerated msg && msg.Type == MessageType.AiMessage && msg.Title == "Model_Error");
        Assert.Contains(publishedMessages, m => m is TriggerAiRunRender msg && msg.ChatKey == "chat-key");
    }

    [Fact]
    public async Task WorkerChatMessageCreatedConsumer_PublishesThinkingMessages_OnCadence()
    {
        var (redis, redisDb) = TestRedisFactory.Create();
        var triggerMessageId = Guid.NewGuid();
        var completionKey = $"ai:completed:message:{triggerMessageId}";
        var completionChecks = 0;
        redisDb.Setup(x => x.KeyExistsAsync(
                It.Is<RedisKey>(key => key.ToString() == completionKey),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(() => Interlocked.Increment(ref completionChecks) >= 4);

        var contextLoader = new Mock<IAiContextLoader>();
        contextLoader.Setup(x => x.LoadAsync(It.IsAny<ChatMessageCreated>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiChatContext
            {
                Chat = new Page.Ui.Domain.Chat.Entities.Chat
                {
                    Id = Guid.NewGuid(),
                    OwnerUserId = "user-1",
                    ChatKey = "chat-key",
                    ModelId = "assistant-default"
                },
                TriggerMessage = new Page.Ui.Domain.Chat.Entities.Message
                {
                    Id = triggerMessageId,
                    ChatId = Guid.NewGuid(),
                    SenderId = "user-1",
                    Content = "hello",
                    MessageKey = "trigger-msg-key"
                },
                History = new List<Page.Ui.Domain.Chat.Entities.Message>()
            });

        var modelClient = new Mock<IAiModelClient>();
        modelClient.Setup(x => x.GenerateAsync(It.IsAny<AiChatContext>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10));
                return AiModelDispatchResult.Success();
            });

        var thinkingProvider = new Mock<IThinkingMessageProvider>();
        thinkingProvider.SetupSequence(x => x.GetRandomMessage())
            .Returns("thinking-one")
            .Returns("thinking-two")
            .Returns("thinking-three");

        var consumer = new Page.Ui.Worker.Ai.Consumers.ChatMessageCreatedConsumer(
            NullLogger<Page.Ui.Worker.Ai.Consumers.ChatMessageCreatedConsumer>.Instance,
            redis.Object,
            contextLoader.Object,
            modelClient.Object,
            Mock.Of<IAiRunStorageService>(),
            thinkingProvider.Object,
            TimeSpan.FromMilliseconds(5),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(25));

        var context = new Mock<ConsumeContext<ChatMessageCreated>>();
        context.SetupGet(x => x.Message).Returns(new ChatMessageCreated(
            triggerMessageId,
            Guid.NewGuid(),
            "chat-key",
            "msg-key",
            "user-1",
            "Room title",
            "hello",
            MessageType.UserMessage,
            DateTimeOffset.UtcNow,
            MessageStatus.Sent,
            null,
            "srv"));
        context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        var thinkingMessages = new List<AiResponseMessageGenerated>();
        context.Setup(x => x.Publish(It.IsAny<AiResponseMessageGenerated>(), It.IsAny<CancellationToken>()))
            .Callback<AiResponseMessageGenerated, CancellationToken>((msg, _) =>
            {
                if (msg.Type == MessageType.Thinking)
                {
                    thinkingMessages.Add(msg);
                }
            })
            .Returns(Task.CompletedTask);

        await consumer.Consume(context.Object);

        Assert.True(thinkingMessages.Count >= 2);
        Assert.Equal("Thinking", thinkingMessages[0].Title);
        Assert.Equal("thinking-one", thinkingMessages[0].Content);
        Assert.Equal("Thinking", thinkingMessages[1].Title);
        Assert.Equal("thinking-two", thinkingMessages[1].Content);
        thinkingProvider.Verify(x => x.GetRandomMessage(), Times.AtLeast(2));
    }

    [Fact]
    public void ThinkingMessageProvider_UsesMultipleBuiltInFallbackMessages_WhenNoMessagesProvided()
    {
        var provider = new ThinkingMessageProvider(Array.Empty<string>());

        var messages = Enumerable.Range(0, 20)
            .Select(_ => provider.GetRandomMessage())
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(messages.Count > 1);
        Assert.NotEqual(new HashSet<string>(["Thinking"], StringComparer.Ordinal), messages);
    }

    [Fact]
    public void ThinkingMessageProvider_DoesNotRepeatConsecutiveMessages_WhenAlternativesExist()
    {
        var provider = new ThinkingMessageProvider(new[] { "one", "two", "three" });

        var previous = provider.GetRandomMessage();
        for (var i = 0; i < 20; i++)
        {
            var next = provider.GetRandomMessage();
            Assert.NotEqual(previous, next);
            previous = next;
        }
    }

    [Fact]
    public async Task RenderErrorReportedConsumer_LoadsRunAndForwardsReport()
    {
        using var appDb = TestDbFactory.CreateContext();
        var chatId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        appDb.Users.Add(new Page.Ui.Domain.Auth.Entities.ApplicationUser
        {
            Id = "user-1",
            UserName = "user-1@example.com",
            NormalizedUserName = "USER-1@EXAMPLE.COM",
            Email = "user-1@example.com",
            NormalizedEmail = "USER-1@EXAMPLE.COM",
            EmailConfirmed = true,
            Name = "User One",
            SecurityStamp = Guid.NewGuid().ToString("N")
        });
        appDb.Chats.Add(new Page.Ui.Domain.Chat.Entities.Chat
        {
            Id = chatId,
            OwnerUserId = "user-1",
            Name = "Room title",
            ChatKey = "chat-key",
            ModelId = "assistant-default"
        });
        appDb.Messages.Add(new Page.Ui.Domain.Chat.Entities.Message
        {
            Id = messageId,
            ChatId = chatId,
            SenderId = "user-1",
            Title = "Room title",
            Content = "hello",
            MessageKey = "trigger-msg-key",
            Type = MessageType.UserMessage,
            Status = MessageStatus.Sent
        });
        appDb.AiRuns.Add(new Page.Ui.Domain.Chat.Entities.AiRun
        {
            Id = runId,
            VersionId = versionId,
            ChatId = chatId,
            OwnerUserId = "user-1",
            TriggerMessageId = messageId,
            ModelId = "assistant-default",
            Title = "Run",
            ManifestObjectKey = "manifest"
        });
        appDb.AiRunFiles.Add(new Page.Ui.Domain.Chat.Entities.AiRunFile
        {
            Id = Guid.NewGuid(),
            RunId = runId,
            ObjectKey = "object-key",
            StoredFileName = "001-index.html",
            OriginalFileName = "index.html",
            ContentType = "text/html",
            Role = "html",
            Sha256 = "sha"
        });
        await appDb.SaveChangesAsync();

        var storage = new Mock<IAiRunStorageService>();
        storage.Setup(x => x.GetObjectContentAsync("object-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync("<main></main>");

        AiErrorReport? capturedReport = null;
        var modelClient = new Mock<IAiModelClient>();
        modelClient.Setup(x => x.ReportErrorAsync(It.IsAny<AiErrorReport>(), It.IsAny<CancellationToken>()))
            .Callback<AiErrorReport, CancellationToken>((report, _) => capturedReport = report)
            .Returns(Task.CompletedTask);

        var consumer = new RenderErrorReportedConsumer(
            appDb,
            storage.Object,
            modelClient.Object,
            NullLogger<RenderErrorReportedConsumer>.Instance);

        var context = new Mock<ConsumeContext<RenderErrorReported>>();
        context.SetupGet(x => x.Message).Returns(new RenderErrorReported(
            chatId,
            versionId,
            "user-1",
            new List<string> { "boom" },
            new List<string> { "warn" }));
        context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(context.Object);

        Assert.NotNull(capturedReport);
        Assert.Equal(chatId, capturedReport!.ChatId);
        Assert.Equal("chat-key", capturedReport.ChatKey);
        Assert.Equal(versionId, capturedReport.VersionId);
        Assert.Equal(messageId, capturedReport.TriggerMessageId);
        Assert.Equal("trigger-msg-key", capturedReport.TriggerMessageKey);
        Assert.Equal("boom", capturedReport.Errors.Single());
        Assert.Equal("<main></main>", capturedReport.SourceFiles.Single().Content);
    }

    [Fact]
    public async Task RenderErrorReportedConsumer_Returns_WhenRunIsMissing()
    {
        using var appDb = TestDbFactory.CreateContext();
        var storage = new Mock<IAiRunStorageService>(MockBehavior.Strict);
        var modelClient = new Mock<IAiModelClient>(MockBehavior.Strict);
        var consumer = new RenderErrorReportedConsumer(
            appDb,
            storage.Object,
            modelClient.Object,
            NullLogger<RenderErrorReportedConsumer>.Instance);

        var context = new Mock<ConsumeContext<RenderErrorReported>>();
        context.SetupGet(x => x.Message).Returns(new RenderErrorReported(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user-1",
            new List<string> { "boom" },
            new List<string>()));
        context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(context.Object);

        modelClient.Verify(x => x.ReportErrorAsync(It.IsAny<AiErrorReport>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
