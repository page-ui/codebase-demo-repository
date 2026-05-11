using MassTransit;
using Microsoft.EntityFrameworkCore;
using Page.Ui.Application.Chat.Contracts;
using Page.Ui.Infrastructure.Auth.Persistence;
using Page.Ui.Worker.Ai.Models;
using Page.Ui.Worker.Ai.Services;

namespace Page.Ui.Worker.Ai.Consumers;

public sealed class RenderErrorReportedConsumer : IConsumer<RenderErrorReported>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAiRunStorageService _storageService;
    private readonly IAiModelClient _aiModelClient;
    private readonly ILogger<RenderErrorReportedConsumer> _logger;

    public RenderErrorReportedConsumer(
        ApplicationDbContext dbContext,
        IAiRunStorageService storageService,
        IAiModelClient aiModelClient,
        ILogger<RenderErrorReportedConsumer> logger)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _aiModelClient = aiModelClient;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RenderErrorReported> context)
    {
        var message = context.Message;
        var run = await _dbContext.AiRuns
            .AsNoTracking()
            .Include(r => r.Chat)
            .Include(r => r.TriggerMessage)
            .Include(r => r.Files)
            .FirstOrDefaultAsync(
                r => r.ChatId == message.ChatId && r.VersionId == message.VersionId,
                context.CancellationToken);

        if (run is null)
        {
            _logger.LogWarning(
                "Render error report skipped because AiRun was not found. ChatId={ChatId} VersionId={VersionId}",
                message.ChatId,
                message.VersionId);
            return;
        }

        var sourceFiles = new List<AiErrorReportSourceFile>();
        foreach (var file in run.Files.OrderBy(file => file.StoredFileName, StringComparer.Ordinal))
        {
            string? content = null;
            string? loadError = null;
            try
            {
                content = await _storageService.GetObjectContentAsync(file.ObjectKey, context.CancellationToken);
            }
            catch (Exception ex)
            {
                loadError = ex.Message;
                _logger.LogWarning(
                    ex,
                    "Failed to load source file {ObjectKey} for render error report. RunId={RunId}",
                    file.ObjectKey,
                    run.Id);
            }

            sourceFiles.Add(new AiErrorReportSourceFile
            {
                FileName = file.StoredFileName,
                ContentType = file.ContentType ?? "text/plain",
                ObjectKey = file.ObjectKey,
                Content = content,
                LoadError = loadError
            });
        }

        await _aiModelClient.ReportErrorAsync(
            new AiErrorReport
            {
                ChatId = run.ChatId,
                ChatKey = run.Chat.ChatKey,
                VersionId = run.VersionId,
                TriggerMessageId = run.TriggerMessageId,
                TriggerMessageKey = run.TriggerMessage?.MessageKey,
                UserId = run.OwnerUserId,
                Errors = message.Errors,
                Logs = message.Logs,
                SourceFiles = sourceFiles
            },
            context.CancellationToken);
    }
}
