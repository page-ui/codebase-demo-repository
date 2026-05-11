using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Page.Ui.Application.Common.Interfaces;
using Page.Ui.Worker.Ai.Configuration;
using Page.Ui.Worker.Ai.Models;

namespace Page.Ui.Worker.Ai.Services;

public sealed class AiModelClient : IAiModelClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiModelApiOptions _options;
    private readonly IInternalServiceJwtProvider _jwtProvider;
    private readonly ILogger<AiModelClient> _logger;

    public AiModelClient(
        IHttpClientFactory httpClientFactory,
        IOptions<AiModelApiOptions> options,
        IInternalServiceJwtProvider jwtProvider,
        ILogger<AiModelClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _jwtProvider = jwtProvider;
        _logger = logger;
    }

    public async Task<AiModelDispatchResult> GenerateAsync(AiChatContext context, string userStorageKey, Guid versionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return AiModelDispatchResult.Failed("The AI model API is not configured right now.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient("AiModelApi");
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.GeneratePath)
            {
                Content = JsonContent.Create(new AiModelApiRequest
                {
                    ChatId = context.Chat.Id,
                    ChatKey = context.Chat.ChatKey,
                    UserStorageKey = userStorageKey,
                    VersionId = versionId,
                    ModelId = context.Chat.ModelId,
                    SystemPrompt = context.Chat.SystemPrompt,
                    ChatName = context.Chat.Name,
                    TriggerMessageId = context.TriggerMessage.Id,
                    TriggerMessageKey = context.TriggerMessage.MessageKey,
                    TriggerMessageContent = context.TriggerMessage.Content,
                    TriggerMessageAttachmentUrl = context.TriggerMessage.AttachmentUrl,
                    History = context.History.Select(m => new AiModelApiMessage
                    {
                        Id = m.Id,
                        SenderId = m.SenderId,
                        MessageKey = m.MessageKey,
                        Title = m.Title,
                        Content = m.Content,
                        AttachmentUrl = m.AttachmentUrl,
                        Type = m.Type.ToString()
                    }).ToList()
                })
            };

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                request.Headers.Add("X-AI-Api-Key", _options.ApiKey);
            }

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                _jwtProvider.CreateAiApiToken(context.Chat.Id, context.TriggerMessage.Id, context.Chat.OwnerUserId));

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI-model-api returned {StatusCode}.", (int)response.StatusCode);
                return AiModelDispatchResult.Failed($"The AI model API returned {(int)response.StatusCode}.");
            }

            return AiModelDispatchResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI-model-api call failed.");
            return AiModelDispatchResult.Failed("The AI model API could not be reached right now.");
        }
    }

    public async Task ReportErrorAsync(AiErrorReport report, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _logger.LogDebug("Skipping render error report because the AI model API is not configured.");
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("AiModelApi");
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.ErrorReportPath)
            {
                Content = JsonContent.Create(new AiModelApiErrorReportRequest
                {
                    ChatId = report.ChatId,
                    ChatKey = report.ChatKey,
                    VersionId = report.VersionId,
                    TriggerMessageId = report.TriggerMessageId,
                    TriggerMessageKey = report.TriggerMessageKey,
                    UserId = report.UserId,
                    Errors = report.Errors,
                    Logs = report.Logs,
                    SourceFiles = report.SourceFiles.Select(file => new AiModelApiErrorReportSourceFile
                    {
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        ObjectKey = file.ObjectKey,
                        Content = file.Content,
                        LoadError = file.LoadError
                    }).ToList()
                })
            };

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                request.Headers.Add("X-AI-Api-Key", _options.ApiKey);
            }

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                _jwtProvider.CreateAiApiToken(report.ChatId, report.TriggerMessageId ?? Guid.Empty, report.UserId));

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "AI-model-api render error report returned {StatusCode} for chat {ChatId} version {VersionId}.",
                    (int)response.StatusCode,
                    report.ChatId,
                    report.VersionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "AI-model-api render error report failed for chat {ChatId} version {VersionId}.",
                report.ChatId,
                report.VersionId);
        }
    }

    private sealed class AiModelApiRequest
    {
        public Guid ChatId { get; init; }
        public string ChatKey { get; init; } = string.Empty;
        public string? UserStorageKey { get; init; }
        public Guid? VersionId { get; init; }
        public string ModelId { get; init; } = string.Empty;
        public string? SystemPrompt { get; init; }
        public string? ChatName { get; init; }
        public Guid TriggerMessageId { get; init; }
        public string TriggerMessageKey { get; init; } = string.Empty;
        public string TriggerMessageContent { get; init; } = string.Empty;
        public string? TriggerMessageAttachmentUrl { get; init; }
        public List<AiModelApiMessage> History { get; init; } = new();
    }

    private sealed class AiModelApiMessage
    {
        public Guid Id { get; init; }
        public string SenderId { get; init; } = string.Empty;
        public string MessageKey { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public string? AttachmentUrl { get; init; }
        public string Type { get; init; } = string.Empty;
    }

    private sealed class AiModelApiErrorReportRequest
    {
        public Guid ChatId { get; init; }
        public string ChatKey { get; init; } = string.Empty;
        public Guid VersionId { get; init; }
        public Guid? TriggerMessageId { get; init; }
        public string? TriggerMessageKey { get; init; }
        public string UserId { get; init; } = string.Empty;
        public List<string> Errors { get; init; } = new();
        public List<string> Logs { get; init; } = new();
        public List<AiModelApiErrorReportSourceFile> SourceFiles { get; init; } = new();
    }

    private sealed class AiModelApiErrorReportSourceFile
    {
        public string FileName { get; init; } = string.Empty;
        public string ContentType { get; init; } = string.Empty;
        public string ObjectKey { get; init; } = string.Empty;
        public string? Content { get; init; }
        public string? LoadError { get; init; }
    }
}
