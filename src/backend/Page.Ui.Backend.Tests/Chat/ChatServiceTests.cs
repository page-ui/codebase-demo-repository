using System.Runtime.Serialization;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Moq;
using Page.Ui.Application.Chat.Contracts;
using Page.Ui.Application.Chat.Inputs;
using Page.Ui.Domain.Chat.Entities;
using Page.Ui.Domain.Auth.Entities;
using Page.Ui.Domain.Chat.Enums;
using Page.Ui.Infrastructure.Chat.Services;
using Page.Ui.Backend.Tests.TestSupport;

namespace Page.Ui.Backend.Tests.Chat;

public class ChatServiceTests
{
    [Fact]
    public async Task CreateChatAsync_PersistsInitialMessage_AndPublishesEvent()
    {
        using var context = TestDbFactory.CreateContext();

        context.Users.Add(new ApplicationUser
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
        await context.SaveChangesAsync();

        var (redis, _) = TestRedisFactory.Create();
        var publish = new Mock<IPublishEndpoint>();
        publish.Setup(x => x.Publish(It.IsAny<ChatMessageCreated>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var minio = new Mock<IMinioClient>();

        var service = new ChatService(
            context,
            publish.Object,
            redis.Object,
            minio.Object,
            NullLogger<ChatService>.Instance);

        var result = await service.CreateChatAsync(
            new CreateChatInput
            {
                Name = "Demo",
                InitialUserMessage = new InitialUserMessageInput
                {
                    Content = "hello world"
                }
            },
            "user-1",
            CancellationToken.None);

        Assert.NotNull(result.Chat);
        Assert.NotNull(result.InitialMessage);
        Assert.Equal(1, context.Chats.Count());
        Assert.Equal(1, context.Messages.Count());
        publish.Verify(
            x => x.Publish(
                It.Is<ChatMessageCreated>(m => m.Content == "hello world" && m.SenderId == "user-1"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateChatAsync_RequiresInitialUserMessage()
    {
        using var context = TestDbFactory.CreateContext();

        context.Users.Add(new ApplicationUser
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
        await context.SaveChangesAsync();

        var (redis, _) = TestRedisFactory.Create();
        var service = new ChatService(
            context,
            Mock.Of<IPublishEndpoint>(),
            redis.Object,
            Mock.Of<IMinioClient>(),
            NullLogger<ChatService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateChatAsync(
                new CreateChatInput
                {
                    Name = "Demo",
                    InitialUserMessage = null!
                },
                "user-1",
                CancellationToken.None));

        Assert.Contains("initialUserMessage", ex.Message);
        Assert.Empty(context.Chats);
        Assert.Empty(context.Messages);
    }

    [Fact]
    public async Task CreateMessageAsync_Fails_WhenUserDoesNotOwnChat()
    {
        using var context = TestDbFactory.CreateContext();

        context.Users.AddRange(
            new ApplicationUser
            {
                Id = "owner",
                UserName = "owner@example.com",
                NormalizedUserName = "OWNER@EXAMPLE.COM",
                Email = "owner@example.com",
                NormalizedEmail = "OWNER@EXAMPLE.COM",
                EmailConfirmed = true,
                Name = "Owner",
                SecurityStamp = Guid.NewGuid().ToString("N")
            },
            new ApplicationUser
            {
                Id = "other",
                UserName = "other@example.com",
                NormalizedUserName = "OTHER@EXAMPLE.COM",
                Email = "other@example.com",
                NormalizedEmail = "OTHER@EXAMPLE.COM",
                EmailConfirmed = true,
                Name = "Other",
                SecurityStamp = Guid.NewGuid().ToString("N")
            });

        var chat = new Page.Ui.Domain.Chat.Entities.Chat
        {
            Id = Guid.NewGuid(),
            OwnerUserId = "owner",
            ChatKey = Guid.NewGuid().ToString("N"),
            ModelId = "assistant-default",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        context.Chats.Add(chat);
        await context.SaveChangesAsync();

        var (redis, _) = TestRedisFactory.Create();
        var service = new ChatService(
            context,
            Mock.Of<IPublishEndpoint>(),
            redis.Object,
            Mock.Of<IMinioClient>(),
            NullLogger<ChatService>.Instance);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateMessageAsync(
                new CreateMessageInput(chat.ChatKey, "hi", null, null),
                "other",
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateMessageAsync_Fails_WhenAttachmentObjectDoesNotExist()
    {
        using var context = TestDbFactory.CreateContext();

        context.Users.Add(new ApplicationUser
        {
            Id = "owner",
            UserName = "owner@example.com",
            NormalizedUserName = "OWNER@EXAMPLE.COM",
            Email = "owner@example.com",
            NormalizedEmail = "OWNER@EXAMPLE.COM",
            EmailConfirmed = true,
            Name = "Owner",
            SecurityStamp = Guid.NewGuid().ToString("N")
        });

        var chat = new Page.Ui.Domain.Chat.Entities.Chat
        {
            Id = Guid.NewGuid(),
            OwnerUserId = "owner",
            ChatKey = Guid.NewGuid().ToString("N"),
            ModelId = "assistant-default",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        context.Chats.Add(chat);
        await context.SaveChangesAsync();

        var minio = new Mock<IMinioClient>();
        var objectNotFound = (ObjectNotFoundException)FormatterServices.GetUninitializedObject(typeof(ObjectNotFoundException));
        minio.Setup(x => x.StatObjectAsync(It.IsAny<StatObjectArgs>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(objectNotFound);

        var (redis, _) = TestRedisFactory.Create();
        var service = new ChatService(
            context,
            Mock.Of<IPublishEndpoint>(),
            redis.Object,
            minio.Object,
            NullLogger<ChatService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateMessageAsync(
                new CreateMessageInput(chat.ChatKey, "hi", null, "http://localhost/minio/chat-uploads/owner/missing.png"),
                "owner",
                CancellationToken.None));

        Assert.Contains("Attachment was not found in storage", ex.Message);
    }

    [Fact]
    public async Task CreateMessageAsync_ReturnsExistingMessage_ForDuplicateClientRequestId()
    {
        using var context = TestDbFactory.CreateContext();

        context.Users.Add(new ApplicationUser
        {
            Id = "owner",
            UserName = "owner@example.com",
            NormalizedUserName = "OWNER@EXAMPLE.COM",
            Email = "owner@example.com",
            NormalizedEmail = "OWNER@EXAMPLE.COM",
            EmailConfirmed = true,
            Name = "Owner",
            SecurityStamp = Guid.NewGuid().ToString("N")
        });

        var chat = new Page.Ui.Domain.Chat.Entities.Chat
        {
            Id = Guid.NewGuid(),
            OwnerUserId = "owner",
            Name = "Room Name",
            ChatKey = Guid.NewGuid().ToString("N"),
            ModelId = "assistant-default",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        context.Chats.Add(chat);
        await context.SaveChangesAsync();

        var publish = new Mock<IPublishEndpoint>();
        publish.Setup(x => x.Publish(It.IsAny<ChatMessageCreated>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var (redis, _) = TestRedisFactory.Create();
        var service = new ChatService(
            context,
            publish.Object,
            redis.Object,
            Mock.Of<IMinioClient>(),
            NullLogger<ChatService>.Instance);

        var input = new CreateMessageInput(chat.ChatKey, "hello", null, null, "req-1");
        var first = await service.CreateMessageAsync(input, "owner", CancellationToken.None);
        var second = await service.CreateMessageAsync(input, "owner", CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal("Room Name", first.Title);
        Assert.Single(context.Messages);
        publish.Verify(x => x.Publish(It.IsAny<ChatMessageCreated>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateMessageAsync_AcceptsMultilineContent()
    {
        using var context = TestDbFactory.CreateContext();

        context.Users.Add(new ApplicationUser
        {
            Id = "owner",
            UserName = "owner@example.com",
            NormalizedUserName = "OWNER@EXAMPLE.COM",
            Email = "owner@example.com",
            NormalizedEmail = "OWNER@EXAMPLE.COM",
            EmailConfirmed = true,
            Name = "Owner",
            SecurityStamp = Guid.NewGuid().ToString("N")
        });

        var chat = new Page.Ui.Domain.Chat.Entities.Chat
        {
            Id = Guid.NewGuid(),
            OwnerUserId = "owner",
            ChatKey = Guid.NewGuid().ToString("N"),
            ModelId = "assistant-default",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        context.Chats.Add(chat);
        await context.SaveChangesAsync();

        var (redis, _) = TestRedisFactory.Create();
        var service = new ChatService(
            context,
            Mock.Of<IPublishEndpoint>(),
            redis.Object,
            Mock.Of<IMinioClient>(),
            NullLogger<ChatService>.Instance);

        var multilineContent = "Line 1\nLine 2\r\nLine 3";
        var result = await service.CreateMessageAsync(
            new CreateMessageInput(chat.ChatKey, multilineContent, null, null),
            "owner",
            CancellationToken.None);

        Assert.Equal("Line 1\nLine 2\nLine 3", result.Content);
        Assert.Single(context.Messages);
    }

    [Fact]
    public async Task CreateChatAsync_AcceptsLongInitialMessageContent()
    {
        using var context = TestDbFactory.CreateContext();

        context.Users.Add(new ApplicationUser
        {
            Id = "owner",
            UserName = "owner@example.com",
            NormalizedUserName = "OWNER@EXAMPLE.COM",
            Email = "owner@example.com",
            NormalizedEmail = "OWNER@EXAMPLE.COM",
            EmailConfirmed = true,
            Name = "Owner",
            SecurityStamp = Guid.NewGuid().ToString("N")
        });
        await context.SaveChangesAsync();

        var publish = new Mock<IPublishEndpoint>();
        publish.Setup(x => x.Publish(It.IsAny<ChatMessageCreated>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var (redis, _) = TestRedisFactory.Create();
        var service = new ChatService(
            context,
            publish.Object,
            redis.Object,
            Mock.Of<IMinioClient>(),
            NullLogger<ChatService>.Instance);

        var longContent = new string('x', 8_000);
        var result = await service.CreateChatAsync(
            new CreateChatInput
            {
                Name = "Long prompt",
                InitialUserMessage = new InitialUserMessageInput
                {
                    Content = longContent
                }
            },
            "owner",
            CancellationToken.None);

        Assert.Equal(longContent, result.InitialMessage!.Content);
    }

    [Fact]
    public async Task CreateMessageAsync_AcceptsLongContent()
    {
        using var context = TestDbFactory.CreateContext();

        context.Users.Add(new ApplicationUser
        {
            Id = "owner",
            UserName = "owner@example.com",
            NormalizedUserName = "OWNER@EXAMPLE.COM",
            Email = "owner@example.com",
            NormalizedEmail = "OWNER@EXAMPLE.COM",
            EmailConfirmed = true,
            Name = "Owner",
            SecurityStamp = Guid.NewGuid().ToString("N")
        });

        var chat = new Page.Ui.Domain.Chat.Entities.Chat
        {
            Id = Guid.NewGuid(),
            OwnerUserId = "owner",
            ChatKey = Guid.NewGuid().ToString("N"),
            ModelId = "assistant-default",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        context.Chats.Add(chat);
        await context.SaveChangesAsync();

        var publish = new Mock<IPublishEndpoint>();
        publish.Setup(x => x.Publish(It.IsAny<ChatMessageCreated>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var (redis, _) = TestRedisFactory.Create();
        var service = new ChatService(
            context,
            publish.Object,
            redis.Object,
            Mock.Of<IMinioClient>(),
            NullLogger<ChatService>.Instance);

        var longContent = new string('x', 8_000);
        var result = await service.CreateMessageAsync(
            new CreateMessageInput(chat.ChatKey, longContent, null, null),
            "owner",
            CancellationToken.None);

        Assert.Equal(longContent, result.Content);
    }

    [Fact]
    public async Task CreateMessageAsync_UsesInputType_WhenProvided()
    {
        using var context = TestDbFactory.CreateContext();

        context.Users.Add(new ApplicationUser
        {
            Id = "owner",
            UserName = "owner@example.com",
            NormalizedUserName = "OWNER@EXAMPLE.COM",
            Email = "owner@example.com",
            NormalizedEmail = "OWNER@EXAMPLE.COM",
            EmailConfirmed = true,
            Name = "Owner",
            SecurityStamp = Guid.NewGuid().ToString("N")
        });

        var chat = new Page.Ui.Domain.Chat.Entities.Chat
        {
            Id = Guid.NewGuid(),
            OwnerUserId = "owner",
            ChatKey = Guid.NewGuid().ToString("N"),
            ModelId = "assistant-default",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        context.Chats.Add(chat);
        await context.SaveChangesAsync();

        ChatMessageCreated? published = null;
        var publish = new Mock<IPublishEndpoint>();
        publish.Setup(x => x.Publish(It.IsAny<ChatMessageCreated>(), It.IsAny<CancellationToken>()))
            .Callback<ChatMessageCreated, CancellationToken>((message, _) => published = message)
            .Returns(Task.CompletedTask);

        var (redis, _) = TestRedisFactory.Create();
        var service = new ChatService(
            context,
            publish.Object,
            redis.Object,
            Mock.Of<IMinioClient>(),
            NullLogger<ChatService>.Instance);

        var result = await service.CreateMessageAsync(
            new CreateMessageInput(chat.ChatKey, "assistant reply", null, null, null, MessageType.AiMessage),
            "owner",
            CancellationToken.None);

        Assert.Equal(MessageType.AiMessage, result.Type);
        Assert.NotNull(published);
        Assert.Equal(MessageType.AiMessage, published!.Type);
    }

    [Fact]
    public async Task CreateMessageAsync_UsesInputIsQuestion_WhenProvided()
    {
        using var context = TestDbFactory.CreateContext();

        context.Users.Add(new ApplicationUser
        {
            Id = "owner",
            UserName = "owner@example.com",
            NormalizedUserName = "OWNER@EXAMPLE.COM",
            Email = "owner@example.com",
            NormalizedEmail = "OWNER@EXAMPLE.COM",
            EmailConfirmed = true,
            Name = "Owner",
            SecurityStamp = Guid.NewGuid().ToString("N")
        });

        var chat = new Page.Ui.Domain.Chat.Entities.Chat
        {
            Id = Guid.NewGuid(),
            OwnerUserId = "owner",
            ChatKey = Guid.NewGuid().ToString("N"),
            ModelId = "assistant-default",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        context.Chats.Add(chat);
        await context.SaveChangesAsync();

        ChatMessageCreated? published = null;
        var publish = new Mock<IPublishEndpoint>();
        publish.Setup(x => x.Publish(It.IsAny<ChatMessageCreated>(), It.IsAny<CancellationToken>()))
            .Callback<ChatMessageCreated, CancellationToken>((message, _) => published = message)
            .Returns(Task.CompletedTask);

        var (redis, _) = TestRedisFactory.Create();
        var service = new ChatService(
            context,
            publish.Object,
            redis.Object,
            Mock.Of<IMinioClient>(),
            NullLogger<ChatService>.Instance);

        var result = await service.CreateMessageAsync(
            new CreateMessageInput(chat.ChatKey, "is this a question?", null, null, IsQuestion: true),
            "owner",
            CancellationToken.None);

        Assert.True(result.IsQuestion);
        Assert.NotNull(published);
        Assert.True(published!.IsQuestion);
    }

    [Fact]
    public async Task ReportRenderErrorAsync_PersistsSanitizedDiagnostics_AndPublishesEvent()
    {
        using var context = TestDbFactory.CreateContext();
        var chatId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        context.Users.Add(new ApplicationUser
        {
            Id = "owner",
            UserName = "owner@example.com",
            NormalizedUserName = "OWNER@EXAMPLE.COM",
            Email = "owner@example.com",
            NormalizedEmail = "OWNER@EXAMPLE.COM",
            EmailConfirmed = true,
            Name = "Owner",
            SecurityStamp = Guid.NewGuid().ToString("N")
        });
        context.Chats.Add(new Page.Ui.Domain.Chat.Entities.Chat
        {
            Id = chatId,
            OwnerUserId = "owner",
            ChatKey = "chat-key",
            ModelId = "assistant-default"
        });
        context.AiRuns.Add(new AiRun
        {
            Id = Guid.NewGuid(),
            VersionId = versionId,
            ChatId = chatId,
            OwnerUserId = "owner",
            ModelId = "assistant-default",
            Title = "Run",
            ManifestObjectKey = "manifest",
            Status = AiRunStatus.Completed
        });
        await context.SaveChangesAsync();

        var publish = new Mock<IPublishEndpoint>();
        publish.Setup(x => x.Publish(It.IsAny<RenderErrorReported>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var (redis, _) = TestRedisFactory.Create();
        var service = new ChatService(
            context,
            publish.Object,
            redis.Object,
            Mock.Of<IMinioClient>(),
            NullLogger<ChatService>.Instance);

        var stored = await service.ReportRenderErrorAsync(
            new ReportRenderErrorInput
            {
                ChatKey = "chat-key",
                VersionId = versionId,
                Errors = new List<string> { " boom\r\n", "   " },
                Logs = new List<string> { "warn\u0001ing" }
            },
            "owner",
            CancellationToken.None);

        Assert.True(stored);
        var run = Assert.Single(context.AiRuns);
        Assert.Contains("boom", run.ClientErrors);
        Assert.Contains("warn ing", run.ClientLogs);
        publish.Verify(
            x => x.Publish(
                It.Is<RenderErrorReported>(report =>
                    report.ChatId == chatId &&
                    report.VersionId == versionId &&
                    report.Errors.Single() == "boom" &&
                    report.Logs.Single() == "warn ing"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReportRenderErrorAsync_RejectsEmptyReports()
    {
        using var context = TestDbFactory.CreateContext();
        var (redis, _) = TestRedisFactory.Create();
        var service = new ChatService(
            context,
            Mock.Of<IPublishEndpoint>(),
            redis.Object,
            Mock.Of<IMinioClient>(),
            NullLogger<ChatService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReportRenderErrorAsync(
                new ReportRenderErrorInput { ChatKey = "chat-key", Errors = new List<string> { " " } },
                "owner",
                CancellationToken.None));
    }

    [Fact]
    public async Task ReportRenderErrorAsync_Fails_WhenUserDoesNotOwnChat()
    {
        using var context = TestDbFactory.CreateContext();
        context.Chats.Add(new Page.Ui.Domain.Chat.Entities.Chat
        {
            Id = Guid.NewGuid(),
            OwnerUserId = "owner",
            ChatKey = "chat-key",
            ModelId = "assistant-default"
        });
        await context.SaveChangesAsync();

        var (redis, _) = TestRedisFactory.Create();
        var service = new ChatService(
            context,
            Mock.Of<IPublishEndpoint>(),
            redis.Object,
            Mock.Of<IMinioClient>(),
            NullLogger<ChatService>.Instance);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ReportRenderErrorAsync(
                new ReportRenderErrorInput { ChatKey = "chat-key", Errors = new List<string> { "boom" } },
                "other",
                CancellationToken.None));
    }

    [Fact]
    public async Task ReportRenderErrorAsync_ReturnsFalse_ForUnknownVersion()
    {
        using var context = TestDbFactory.CreateContext();
        context.Chats.Add(new Page.Ui.Domain.Chat.Entities.Chat
        {
            Id = Guid.NewGuid(),
            OwnerUserId = "owner",
            ChatKey = "chat-key",
            ModelId = "assistant-default"
        });
        await context.SaveChangesAsync();

        var publish = new Mock<IPublishEndpoint>(MockBehavior.Strict);
        var (redis, _) = TestRedisFactory.Create();
        var service = new ChatService(
            context,
            publish.Object,
            redis.Object,
            Mock.Of<IMinioClient>(),
            NullLogger<ChatService>.Instance);

        var stored = await service.ReportRenderErrorAsync(
            new ReportRenderErrorInput
            {
                ChatKey = "chat-key",
                VersionId = Guid.NewGuid(),
                Errors = new List<string> { "boom" }
            },
            "owner",
            CancellationToken.None);

        Assert.False(stored);
    }
}
