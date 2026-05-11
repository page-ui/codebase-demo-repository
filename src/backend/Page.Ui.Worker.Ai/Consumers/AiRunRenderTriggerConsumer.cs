using MassTransit;
using Microsoft.EntityFrameworkCore;
using Page.Ui.Application.Chat.Contracts;
using Page.Ui.Domain.Chat.Entities;
using Page.Ui.Domain.Chat.Enums;
using Page.Ui.Worker.Ai.Models;
using Page.Ui.Worker.Ai.Services;
using StackExchange.Redis;

namespace Page.Ui.Worker.Ai.Consumers;

public class AiRunRenderTriggerConsumer : IConsumer<TriggerAiRunRender>
{
    private readonly IAiRunStorageService _storageService;
    private readonly IAiContextLoader _contextLoader;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiRunRenderTriggerConsumer> _logger;
    private readonly Page.Ui.Infrastructure.Auth.Persistence.ApplicationDbContext _dbContext;
    private readonly IConnectionMultiplexer _redis;

    public AiRunRenderTriggerConsumer(
        IAiRunStorageService storageService,
        IAiContextLoader contextLoader,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AiRunRenderTriggerConsumer> logger,
        Page.Ui.Infrastructure.Auth.Persistence.ApplicationDbContext dbContext,
        IConnectionMultiplexer redis)
    {
        _storageService = storageService;
        _contextLoader = contextLoader;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _dbContext = dbContext;
        _redis = redis;
    }

    public async Task Consume(ConsumeContext<TriggerAiRunRender> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing external render trigger for RunId {RunId}", msg.RunId);

        try
        {
            var chatContext = await _contextLoader.LoadAsync(new ChatMessageCreated { Id = msg.ReplyToMessageId, ChatId = msg.ChatId }, context.CancellationToken);
            if (chatContext == null)
            {
                _logger.LogWarning("Failed to load chat context for render trigger {RunId}", msg.RunId);
                return;
            }

            var aiResult = new AiModelResult
            {
                Files = msg.Files.Select(f => new AiSourceFile
                {
                    FileName = f.FileName,
                    ContentType = f.ContentType,
                    ObjectKey = f.ObjectKey
                }).ToList()
            };

            var storedRun = await _storageService.StoreAsync(chatContext, aiResult, msg.VersionId, msg.RunId, context.CancellationToken);
            storedRun.Run.Status = AiRunStatus.Rendering;
            await _dbContext.SaveChangesAsync(context.CancellationToken);

            var renderAttempt = await TryRenderPreviewAsync(
                msg.ReplyToMessageId,
                msg.ChatId,
                msg.ChatKey,
                msg.RunId,
                msg.VersionId,
                msg.UserStorageKey,
                storedRun.Files,
                context.CancellationToken);

            if (!string.IsNullOrWhiteSpace(renderAttempt.PreviewUrl))
            {
                storedRun.Run.Status = AiRunStatus.Completed;
                storedRun.Run.FinalPreviewUrl = renderAttempt.PreviewUrl;
                storedRun.Run.CompletedAt = DateTimeOffset.UtcNow;
                await _dbContext.SaveChangesAsync(context.CancellationToken);
                await _storageService.PromoteCurrentAsync(storedRun, context.CancellationToken);

                await context.Publish(new AiResponseMessageGenerated(
                    msg.ChatId,
                    storedRun.Run.Title,
                    renderAttempt.PreviewUrl,
                    MessageType.AiRun,
                    msg.ReplyToMessageId,
                    msg.RunId,
                    msg.VersionId));
            }
            else
            {
                await _storageService.MarkFailedAsync(storedRun, "render_failed", renderAttempt.ErrorMessage, context.CancellationToken);
                
                await context.Publish(new AiResponseMessageGenerated(
                    msg.ChatId,
                    storedRun.Run.Title,
                    renderAttempt.ErrorMessage,
                    MessageType.AiMessage,
                    msg.ReplyToMessageId,
                    msg.RunId,
                    msg.VersionId));
            }

            await MarkTriggerMessageCompletedAsync(msg.ReplyToMessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External render trigger failed for RunId {RunId}", msg.RunId);
            throw;
        }
    }

    private async Task MarkTriggerMessageCompletedAsync(Guid messageId)
    {
        await _redis
            .GetDatabase()
            .StringSetAsync($"ai:completed:message:{messageId}", "1", TimeSpan.FromHours(6));
    }

    private async Task<RenderAttemptResult> TryRenderPreviewAsync(
        Guid messageId, Guid chatId, string chatKey, Guid runId, Guid versionId, string userStorageKey,
        IReadOnlyList<AiRunFile> files, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("SvelteRender");
            var pages = BuildRenderPages(files);
            var requestBody = new {
                Pages = pages,
                SourceFiles = files.Select(file => new RenderSourceFileDto
                {
                    FileName = file.StoredFileName,
                    ContentType = file.ContentType,
                    ObjectKey = file.ObjectKey
                }).ToList(),
                Metadata = new Dictionary<string, string> {
                    ["userId"] = "worker-ai",
                    ["chatId"] = chatId.ToString(),
                    ["messageId"] = messageId.ToString(),
                    ["userStorageKey"] = userStorageKey,
                    ["chatKey"] = chatKey,
                    ["runId"] = runId.ToString(),
                    ["versionId"] = versionId.ToString()
                }
            };

            using var response = await client.PostAsJsonAsync("api/render-objects", requestBody, cancellationToken);
            if (!response.IsSuccessStatusCode) return RenderAttemptResult.Failed("RenderError: Server error.");

            var result = await response.Content.ReadFromJsonAsync<RenderResponseDto>(cancellationToken: cancellationToken);
            if (result == null || result.Errors.Count > 0) return RenderAttemptResult.Failed("RenderError: Compilation failed.");

            return RenderAttemptResult.Succeeded(BuildPublicPreviewUrl(result.PreviewUrl));
        }
        catch (Exception ex)
        {
            return RenderAttemptResult.Failed($"RenderError: {ex.Message}");
        }
    }

    private string BuildPublicPreviewUrl(string previewPath)
    {
        if (Uri.TryCreate(previewPath, UriKind.Absolute, out _)) return previewPath;
        var publicBaseUrl = _configuration["SvelteRender:PublicBaseUrl"] ?? _configuration["SvelteRender__PublicBaseUrl"];
        return string.IsNullOrWhiteSpace(publicBaseUrl) ? previewPath : $"{publicBaseUrl.TrimEnd('/')}/{previewPath.TrimStart('/')}";
    }

    private static List<RenderObjectPageDto> BuildRenderPages(IReadOnlyList<AiRunFile> files)
    {
        var htmlFiles = files
            .Where(file => file.Role == "html")
            .OrderBy(file => file.StoredFileName, StringComparer.Ordinal)
            .ToList();
        var cssFiles = files
            .Where(file => file.Role == "css")
            .OrderBy(file => file.StoredFileName, StringComparer.Ordinal)
            .ToList();
        var jsFiles = files
            .Where(file => file.Role == "js")
            .OrderBy(file => file.StoredFileName, StringComparer.Ordinal)
            .ToList();

        var pages = htmlFiles.Count > 0
            ? htmlFiles.Select(file => new RenderObjectPageDto
            {
                Path = NormalizePagePath(file.StoredFileName),
                HtmlObjectKey = file.ObjectKey
            }).ToList()
            : new List<RenderObjectPageDto> { new() { Path = "index" } };

        foreach (var file in cssFiles)
        {
            AttachSource(pages, file, isCss: true);
        }

        foreach (var file in jsFiles)
        {
            AttachSource(pages, file, isCss: false);
        }

        return pages;
    }

    private static void AttachSource(List<RenderObjectPageDto> pages, AiRunFile file, bool isCss)
    {
        var targetPath = NormalizePagePath(file.StoredFileName);
        var target = pages.FirstOrDefault(page => page.Path == targetPath);

        if (target == null && pages.Count == 1)
        {
            target = pages[0];
        }

        if (target == null)
        {
            target = pages.FirstOrDefault(page => page.Path == "index") ?? pages[0];
        }

        if (isCss && string.IsNullOrWhiteSpace(target.CssObjectKey))
        {
            target.CssObjectKey = file.ObjectKey;
        }
        else if (!isCss && string.IsNullOrWhiteSpace(target.JsObjectKey))
        {
            target.JsObjectKey = file.ObjectKey;
        }
    }

    private static string NormalizePagePath(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName).Trim().ToLowerInvariant();
        if (stem.Length > 4 && char.IsDigit(stem[0]) && char.IsDigit(stem[1]) && char.IsDigit(stem[2]) && stem[3] == '-')
        {
            stem = stem[4..];
        }

        stem = stem switch
        {
            "style" or "styles" or "script" or "scripts" or "app" or "main" => "index",
            _ => stem
        };

        var slug = string.Concat(stem.Select(ch => char.IsAsciiLetterOrDigit(ch) ? ch : '-')).Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "index" : slug;
    }

    private sealed class RenderResponseDto { public string PreviewUrl { get; set; } = string.Empty; public List<string> Errors { get; set; } = new(); }
    private sealed class RenderObjectPageDto
    {
        public string Path { get; set; } = "index";
        public string? HtmlObjectKey { get; set; }
        public string? CssObjectKey { get; set; }
        public string? JsObjectKey { get; set; }
    }

    private sealed class RenderSourceFileDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string ObjectKey { get; set; } = string.Empty;
    }

    private sealed class RenderAttemptResult { 
        public string? PreviewUrl { get; set; } 
        public string ErrorMessage { get; set; } = string.Empty;
        public static RenderAttemptResult Succeeded(string url) => new() { PreviewUrl = url };
        public static RenderAttemptResult Failed(string err) => new() { ErrorMessage = err };
    }
}
