using MassTransit;
using Page.Ui.Application.Chat.Contracts;
using Page.Ui.Domain.Chat;
using Page.Ui.Domain.Chat.Enums;
using Page.Ui.Worker.Ai.Models;
using Page.Ui.Worker.Ai.Services;
using StackExchange.Redis;

namespace Page.Ui.Worker.Ai.Consumers;

public class ChatMessageCreatedConsumer : IConsumer<ChatMessageCreated>
{
    private static readonly TimeSpan DefaultThinkingCompletionWaitTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DefaultThinkingCompletionPollInterval = TimeSpan.FromSeconds(1);

    private readonly ILogger<ChatMessageCreatedConsumer> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IAiContextLoader _contextLoader;
    private readonly IAiModelClient _aiModelClient;
    private readonly IAiRunStorageService _aiRunStorageService;
    private readonly IThinkingMessageProvider _thinkingMessageProvider;
    private readonly TimeSpan _thinkingInterval;
    private readonly TimeSpan _thinkingCompletionWaitTimeout;
    private readonly TimeSpan _thinkingCompletionPollInterval;

    public ChatMessageCreatedConsumer(
        ILogger<ChatMessageCreatedConsumer> logger,
        IConnectionMultiplexer redis,
        IAiContextLoader contextLoader,
        IAiModelClient aiModelClient,
        IAiRunStorageService aiRunStorageService,
        IThinkingMessageProvider thinkingMessageProvider)
        : this(logger, redis, contextLoader, aiModelClient, aiRunStorageService, thinkingMessageProvider, TimeSpan.FromSeconds(20))
    {
    }

    public ChatMessageCreatedConsumer(
        ILogger<ChatMessageCreatedConsumer> logger,
        IConnectionMultiplexer redis,
        IAiContextLoader contextLoader,
        IAiModelClient aiModelClient,
        IAiRunStorageService aiRunStorageService,
        IThinkingMessageProvider thinkingMessageProvider,
        TimeSpan thinkingInterval,
        TimeSpan? thinkingCompletionWaitTimeout = null,
        TimeSpan? thinkingCompletionPollInterval = null)
    {
        _logger = logger;
        _redis = redis;
        _contextLoader = contextLoader;
        _aiModelClient = aiModelClient;
        _aiRunStorageService = aiRunStorageService;
        _thinkingMessageProvider = thinkingMessageProvider;
        _thinkingInterval = thinkingInterval;
        _thinkingCompletionWaitTimeout = thinkingCompletionWaitTimeout ?? DefaultThinkingCompletionWaitTimeout;
        _thinkingCompletionPollInterval = thinkingCompletionPollInterval ?? DefaultThinkingCompletionPollInterval;
    }

    public async Task Consume(ConsumeContext<ChatMessageCreated> context)
    {
        if (context.Message.SenderId == ChatConstants.AiBotUserId || context.Message.Type != MessageType.UserMessage)
        {
            _logger.LogDebug("Skipping AI worker processing for non-user prompt message {MessageId}", context.Message.Id);
            return;
        }

        var db = _redis.GetDatabase();
        var rateLimitKey = $"ratelimit:ai:{context.Message.SenderId}";
        if (await db.KeyExistsAsync(rateLimitKey))
        {
            _logger.LogWarning("Rate limit hit for user {UserId}, skipping AI response", context.Message.SenderId);
            return;
        }

        await db.StringSetAsync(rateLimitKey, "1", TimeSpan.FromSeconds(2));

        var contextKey = $"ai:processing:message:{context.Message.Id}";
        if (!await db.StringSetAsync(contextKey, "1", TimeSpan.FromMinutes(5), false, When.NotExists))
        {
            _logger.LogInformation("Message {MessageId} is already being processed; skipping duplicate delivery.", context.Message.Id);
            return;
        }

        var completionKey = BuildCompletionKey(context.Message.Id);

        try
        {
            var chatContext = await _contextLoader.LoadAsync(context.Message, context.CancellationToken);
            if (chatContext is null)
            {
                _logger.LogWarning("Unable to load AI chat context for message {MessageId}", context.Message.Id);
                return;
            }

            var userStorageKey = BuildUserStorageKey(chatContext.Chat.OwnerUserId);
            var versionId = Guid.NewGuid();
            var runId = Guid.NewGuid();

            await PublishThinkingAsync(context, context.CancellationToken);
            var dispatchTask = _aiModelClient.GenerateAsync(chatContext, userStorageKey, versionId, context.CancellationToken);
            var dispatchResult = await WaitForDispatchAsync(context, dispatchTask, context.CancellationToken);

            if (!dispatchResult.Accepted)
            {
                await PublishFallbackAsync(
                    context,
                    chatContext,
                    userStorageKey,
                    versionId,
                    runId,
                    dispatchResult.FailureMessage ?? "The AI model API failed to generate a run.");
                await db.StringSetAsync(completionKey, "1", TimeSpan.FromHours(6));
            }
            else if (!await PublishThinkingUntilCompletionAsync(db, context, completionKey, context.CancellationToken))
            {
                _logger.LogWarning(
                    "Timed out waiting for AI render completion for message {MessageId}; stopping thinking updates.",
                    context.Message.Id);
            }
        }
        finally
        {
            await db.KeyDeleteAsync(contextKey);
        }
    }

    private static string BuildUserStorageKey(string userId)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(userId.Trim().ToLowerInvariant()));
        return Convert.ToHexString(hash)[..24].ToLowerInvariant();
    }

    private static string BuildCompletionKey(Guid messageId)
    {
        return $"ai:completed:message:{messageId}";
    }

    private async Task<AiModelDispatchResult> WaitForDispatchAsync(
        ConsumeContext<ChatMessageCreated> context,
        Task<AiModelDispatchResult> dispatchTask,
        CancellationToken cancellationToken)
    {
        while (!dispatchTask.IsCompleted)
        {
            await Task.Delay(_thinkingInterval, cancellationToken);
            if (!dispatchTask.IsCompleted)
            {
                await PublishThinkingAsync(context, cancellationToken);
            }
        }

        return await dispatchTask;
    }

    private async Task<bool> PublishThinkingUntilCompletionAsync(
        IDatabase db,
        ConsumeContext<ChatMessageCreated> context,
        string completionKey,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.Add(_thinkingCompletionWaitTimeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await db.KeyExistsAsync(completionKey))
            {
                return true;
            }

            try
            {
                await Task.Delay(_thinkingInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            if (await db.KeyExistsAsync(completionKey))
            {
                return true;
            }

            await PublishThinkingAsync(context, cancellationToken);
        }

        _logger.LogDebug(
            "AI completion key {CompletionKey} was not set before timeout for message {MessageId}.",
            completionKey,
            context.Message.Id);
        return false;
    }

    private async Task PublishThinkingAsync(ConsumeContext<ChatMessageCreated> context, CancellationToken cancellationToken)
    {
        var thinkingMsg = _thinkingMessageProvider.GetRandomMessage();
        _logger.LogInformation(
            "Publishing thinking update for message {MessageId}: {ThinkingMessage}",
            context.Message.Id,
            thinkingMsg);

        await context.Publish(new AiResponseMessageGenerated(
            context.Message.ChatId,
            "Thinking",
            thinkingMsg,
            MessageType.Thinking,
            context.Message.Id,
            null,
            null,
            false
        ), cancellationToken);
    }

    private async Task PublishFallbackAsync(
        ConsumeContext<ChatMessageCreated> context,
        AiChatContext chatContext,
        string userStorageKey,
        Guid versionId,
        Guid runId,
        string failureMessage)
    {
        var safeFailureMessage = string.IsNullOrWhiteSpace(failureMessage)
            ? "The run could not be generated right now. Please retry."
            : $"{failureMessage.Trim()} Please retry.";

        await context.Publish(new AiResponseMessageGenerated(
            chatContext.Chat.Id,
            "Model_Error",
            safeFailureMessage,
            Page.Ui.Domain.Chat.Enums.MessageType.AiMessage,
            chatContext.TriggerMessage.Id,
            runId,
            versionId));

        var fallbackResult = BuildModelErrorFallback(chatContext, safeFailureMessage);
        var storedRun = await _aiRunStorageService.StoreAsync(chatContext, fallbackResult, versionId, runId, context.CancellationToken);

        await context.Publish(new TriggerAiRunRender(
            chatContext.Chat.Id,
            chatContext.Chat.ChatKey,
            chatContext.TriggerMessage.Id,
            runId,
            versionId,
            userStorageKey,
            storedRun.Files.Select(file => new AiSourceFileDto(
                file.StoredFileName,
                file.ContentType ?? "text/plain",
                file.ObjectKey)).ToList()));
    }

    private static AiModelResult BuildModelErrorFallback(AiChatContext chatContext, string failureMessage)
    {
        var originalPrompt = chatContext.TriggerMessage.Content.Replace("\r\n", "\n").Trim();
        if (string.IsNullOrWhiteSpace(originalPrompt))
        {
            originalPrompt = "No prompt payload detected.";
        }

        var promptPayload = $"{failureMessage}\n\nOriginal prompt:\n{originalPrompt}";
        var shortChatId = chatContext.Chat.Id.ToString("N")[..8];
        var shortMessageId = chatContext.TriggerMessage.Id.ToString("N")[..8];
        var createdAtUtc = chatContext.TriggerMessage.CreatedAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

        return new AiModelResult
        {
            Title = "Model_Error",
            AssistantMessage = failureMessage,
            IsQuestion = false,
            ShouldGenerateUi = true,
            Files = new[]
            {
                new AiSourceFile
                {
                    FileName = "001-index.html",
                    ContentType = "text/html",
                    Content = WorkerRenderTemplates.BuildRetroCliHtml(promptPayload, createdAtUtc, shortChatId, shortMessageId)
                },
                new AiSourceFile
                {
                    FileName = "002-styles.css",
                    ContentType = "text/css",
                    Content = WorkerRenderTemplates.BuildRetroCliCss()
                },
                new AiSourceFile
                {
                    FileName = "003-app.js",
                    ContentType = "application/javascript",
                    Content = string.Empty
                }
            }
        };
    }
}
