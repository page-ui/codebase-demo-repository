using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Page.Ui.Application.Chat.Contracts;
using Page.Ui.Backend.Tests.TestSupport;
using Page.Ui.Application.Common.Interfaces;
using Page.Ui.Domain.Chat.Entities;
using Page.Ui.Domain.Chat.Enums;
using Page.Ui.Presentation.Chat.Controllers;
using Page.Ui.Presentation.Chat.GraphQl.Inputs;
using Page.Ui.Presentation.Chat.GraphQl.Mutations;
using Page.Ui.Worker.Ai.Configuration;
using Page.Ui.Worker.Ai.Consumers;
using Page.Ui.Worker.Ai.Models;
using Page.Ui.Worker.Ai.Services;
using Minio;
using Minio.DataModel.Args;

namespace Page.Ui.Backend.Tests.Chat;

public class AiApiWorkflowTests
{
    [Fact]
    public async Task AiModelClient_GenerateAsync_SendsExpectedPayload_AndAcceptsAcceptedResponse()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedPayload = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            capturedPayload = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://ai-api/") };
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient("AiModelApi")).Returns(httpClient);

        var jwtProvider = new Mock<IInternalServiceJwtProvider>();
        jwtProvider.Setup(x => x.CreateAiApiToken(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns("signed-token");

        var client = new AiModelClient(
            httpClientFactory.Object,
            Options.Create(new AiModelApiOptions
            {
                BaseUrl = "http://ai-api/",
                GeneratePath = "api/generate",
                TimeoutSeconds = 30
            }),
            jwtProvider.Object,
            NullLogger<AiModelClient>.Instance);

        var context = CreateAiChatContext();
        var versionId = Guid.NewGuid();

        var result = await client.GenerateAsync(context, "storage-key", versionId, CancellationToken.None);

        Assert.True(result.Accepted);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization?.Scheme);
        Assert.Equal("signed-token", capturedRequest.Headers.Authorization?.Parameter);

        Assert.NotNull(capturedPayload);
        Assert.Contains("\"chatKey\":\"chat-key\"", capturedPayload);
        Assert.Contains("\"triggerMessageKey\":\"trigger-msg-key\"", capturedPayload);
        Assert.Contains("\"userStorageKey\":\"storage-key\"", capturedPayload);
        Assert.Contains(versionId.ToString("D"), capturedPayload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AiModelClient_ReportErrorAsync_SendsExpectedPayload()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedPayload = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            capturedPayload = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://ai-api/") };
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient("AiModelApi")).Returns(httpClient);

        var jwtProvider = new Mock<IInternalServiceJwtProvider>();
        jwtProvider.Setup(x => x.CreateAiApiToken(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns("signed-token");

        var client = new AiModelClient(
            httpClientFactory.Object,
            Options.Create(new AiModelApiOptions
            {
                BaseUrl = "http://ai-api/",
                ApiKey = "api-key",
                ErrorReportPath = "api/report-error",
                TimeoutSeconds = 30
            }),
            jwtProvider.Object,
            NullLogger<AiModelClient>.Instance);

        var chatId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var triggerMessageId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var versionId = Guid.NewGuid();

        await client.ReportErrorAsync(
            new AiErrorReport
            {
                ChatId = chatId,
                ChatKey = "chat-key",
                VersionId = versionId,
                TriggerMessageId = triggerMessageId,
                TriggerMessageKey = "trigger-msg-key",
                UserId = "user-1",
                Errors = new List<string> { "boom" },
                Logs = new List<string> { "warn" },
                SourceFiles = new List<AiErrorReportSourceFile>
                {
                    new()
                    {
                        FileName = "001-index.html",
                        ContentType = "text/html",
                        ObjectKey = "users/storage/chats/chat-key/versions/version/source/001-index.html",
                        Content = "<main></main>"
                    }
                }
            },
            CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.EndsWith("api/report-error", capturedRequest.RequestUri!.ToString(), StringComparison.Ordinal);
        Assert.Equal("api-key", capturedRequest.Headers.GetValues("X-AI-Api-Key").Single());
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization?.Scheme);
        Assert.Equal("signed-token", capturedRequest.Headers.Authorization?.Parameter);
        Assert.NotNull(capturedPayload);
        Assert.Contains("\"chatKey\":\"chat-key\"", capturedPayload);
        Assert.Contains("\"versionId\":\"" + versionId.ToString("D"), capturedPayload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"errors\":[\"boom\"]", capturedPayload);
        Assert.Contains("\"sourceFiles\"", capturedPayload);
        jwtProvider.Verify(x => x.CreateAiApiToken(chatId, triggerMessageId, "user-1"), Times.Once);
    }

    [Fact]
    public async Task ChatMutations_CreateMessage_RejectsInternalAiReplyTargetMismatch()
    {
        using var db = TestDbFactory.CreateContext();
        var chat = SeedOwnedChat(db);
        var triggerMessage = SeedTriggerMessage(chat.Id);
        db.Messages.Add(triggerMessage);
        await db.SaveChangesAsync();

        var chatService = new Mock<Page.Ui.Application.Chat.Services.IChatService>(MockBehavior.Strict);
        var currentUser = BuildInternalAiPrincipal("user-1", chat.Id, triggerMessage.Id);
        var sut = new ChatMutations();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.CreateMessage(
            new PublicCreateMessageInput(chat.ChatKey, "reply", "wrong-key", null),
            chatService.Object,
            db,
            currentUser,
            CancellationToken.None));
    }

    [Fact]
    public async Task AiDevUploadController_GetPresignedUrl_UsesClaimBoundPrefix()
    {
        using var db = TestDbFactory.CreateContext();
        var chat = SeedOwnedChat(db);
        var triggerMessage = SeedTriggerMessage(chat.Id);
        db.Messages.Add(triggerMessage);
        await db.SaveChangesAsync();

        var minio = new Mock<IMinioClient>();
        minio.Setup(x => x.BucketExistsAsync(It.IsAny<BucketExistsArgs>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        minio.Setup(x => x.PresignedPutObjectAsync(It.IsAny<PresignedPutObjectArgs>()))
            .ReturnsAsync("http://minio:9000/ai-runs/upload");
        minio.Setup(x => x.PresignedGetObjectAsync(It.IsAny<PresignedGetObjectArgs>()))
            .ReturnsAsync("http://minio:9000/ai-runs/download");

        var (redis, _) = TestRedisFactory.Create();
        var publishEndpoint = new Mock<IPublishEndpoint>();
        var controller = new AiDevUploadController(
            minio.Object,
            redis.Object,
            publishEndpoint.Object,
            db,
            NullLogger<AiDevUploadController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = BuildInternalAiPrincipal("user-1", chat.Id, triggerMessage.Id)
            }
        };

        var versionId = Guid.NewGuid();
        var userStorageKey = BuildUserStorageKey("user-1");
        var actionResult = await controller.GetPresignedUrl(userStorageKey, chat.ChatKey, versionId.ToString("D"), "index.html");

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var objectKey = ok.Value!.GetType().GetProperty("objectKey")!.GetValue(ok.Value) as string;
        Assert.Equal($"users/{userStorageKey}/chats/{chat.ChatKey}/versions/{versionId:D}/source/index.html", objectKey);
    }

    [Fact]
    public async Task AiDevUploadController_TriggerRender_RejectsFilesOutsideAllowedPrefix()
    {
        using var db = TestDbFactory.CreateContext();
        var chat = SeedOwnedChat(db);
        var triggerMessage = SeedTriggerMessage(chat.Id);
        db.Messages.Add(triggerMessage);
        await db.SaveChangesAsync();

        var minio = new Mock<IMinioClient>(MockBehavior.Strict);
        var (redis, _) = TestRedisFactory.Create();
        var publishEndpoint = new Mock<IPublishEndpoint>(MockBehavior.Strict);
        var controller = new AiDevUploadController(
            minio.Object,
            redis.Object,
            publishEndpoint.Object,
            db,
            NullLogger<AiDevUploadController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = BuildInternalAiPrincipal("user-1", chat.Id, triggerMessage.Id)
            }
        };

        var userStorageKey = BuildUserStorageKey("user-1");
        var result = await controller.TriggerRender(new AiDevUploadController.RenderTriggerInput(
            chat.Id,
            chat.ChatKey,
            triggerMessage.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            userStorageKey,
            new List<AiSourceFileDto>
            {
                new("001-index.html", "text/html", "users/other/chats/chat-key/versions/version/source/001-index.html")
            }));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AiRunRenderTriggerConsumer_SendsRenderApiKeyHeader()
    {
        using var db = TestDbFactory.CreateContext();
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = System.Net.Http.Json.JsonContent.Create(new
                {
                    previewUrl = "/runs/public-token/preview.html",
                    errors = Array.Empty<string>()
                })
            };
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://render/") };
        httpClient.DefaultRequestHeaders.Add("X-Render-Api-Key", "render-key");

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient("SvelteRender")).Returns(httpClient);

        var contextLoader = new Mock<IAiContextLoader>();
        contextLoader.Setup(x => x.LoadAsync(It.IsAny<ChatMessageCreated>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAiChatContext());

        var storage = new Mock<IAiRunStorageService>();
        storage.Setup(x => x.StoreAsync(It.IsAny<AiChatContext>(), It.IsAny<AiModelResult>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredAiRun
            {
                Run = new AiRun
                {
                    Id = Guid.NewGuid(),
                    VersionId = Guid.NewGuid(),
                    ChatId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    OwnerUserId = "user-1",
                    TriggerMessageId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    ModelId = "assistant-default",
                    Title = "Generated UI",
                    ManifestObjectKey = "manifest"
                },
                Files = new List<Page.Ui.Domain.Chat.Entities.AiRunFile>()
            });
        storage.Setup(x => x.LoadRenderInputsAsync(It.IsAny<StoredAiRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("<html></html>", "body{}", string.Empty));
        storage.Setup(x => x.PromoteCurrentAsync(It.IsAny<StoredAiRun>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var (redis, _) = TestRedisFactory.Create();

        var consumer = new AiRunRenderTriggerConsumer(
            storage.Object,
            contextLoader.Object,
            httpClientFactory.Object,
            new ConfigurationBuilder().Build(),
            NullLogger<AiRunRenderTriggerConsumer>.Instance,
            db,
            redis.Object);

        var consumeContext = new Mock<ConsumeContext<TriggerAiRunRender>>();
        consumeContext.SetupGet(x => x.Message).Returns(new TriggerAiRunRender(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "chat-key",
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.NewGuid(),
            Guid.NewGuid(),
            BuildUserStorageKey("user-1"),
            new List<AiSourceFileDto>
            {
                new("001-index.html", "text/html", "users/storage/chats/chat-key/versions/version/source/001-index.html")
            }));
        consumeContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);
        consumeContext.Setup(x => x.Publish(It.IsAny<AiResponseMessageGenerated>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await consumer.Consume(consumeContext.Object);

        Assert.NotNull(capturedRequest);
        Assert.Equal("render-key", capturedRequest!.Headers.GetValues("X-Render-Api-Key").Single());
    }

    private static Page.Ui.Domain.Chat.Entities.Chat SeedOwnedChat(Page.Ui.Infrastructure.Auth.Persistence.ApplicationDbContext db)
    {
        db.Users.Add(new Page.Ui.Domain.Auth.Entities.ApplicationUser
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

        var chat = new Page.Ui.Domain.Chat.Entities.Chat
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            OwnerUserId = "user-1",
            Name = "Room title",
            ChatKey = "chat-key",
            ModelId = "assistant-default",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Chats.Add(chat);
        return chat;
    }

    private static Message SeedTriggerMessage(Guid chatId)
    {
        return new Message
        {
            Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            ChatId = chatId,
            SenderId = "user-1",
            Title = "Room title",
            Content = "hello",
            Type = MessageType.UserMessage,
            Status = MessageStatus.Sent,
            MessageKey = "trigger-msg-key",
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static ClaimsPrincipal BuildInternalAiPrincipal(string userId, Guid chatId, Guid messageId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim("sub", "worker-ai"),
                new Claim("user_id", userId),
                new Claim("chat_id", chatId.ToString()),
                new Claim("message_id", messageId.ToString())
            },
            "InternalService"));
    }

    private static string BuildUserStorageKey(string userId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(userId.Trim().ToLowerInvariant()));
        return Convert.ToHexString(hash)[..24].ToLowerInvariant();
    }

    private static AiChatContext CreateAiChatContext()
    {
        var chatId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var triggerMessageId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var chat = new Page.Ui.Domain.Chat.Entities.Chat
        {
            Id = chatId,
            OwnerUserId = "user-1",
            Name = "Room title",
            ChatKey = "chat-key",
            ModelId = "assistant-default",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var triggerMessage = new Message
        {
            Id = triggerMessageId,
            ChatId = chatId,
            SenderId = "user-1",
            Title = "Room title",
            Content = "Make a dashboard",
            Type = MessageType.UserMessage,
            Status = MessageStatus.Sent,
            MessageKey = "trigger-msg-key",
            CreatedAt = DateTimeOffset.UtcNow
        };

        return new AiChatContext
        {
            Chat = chat,
            TriggerMessage = triggerMessage,
            History = new[] { triggerMessage }
        };
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }
}
