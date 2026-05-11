using Page.Ui.Worker.Ai.Models;

namespace Page.Ui.Worker.Ai.Services;

public interface IAiModelClient
{
    Task<AiModelDispatchResult> GenerateAsync(AiChatContext context, string userStorageKey, Guid versionId, CancellationToken cancellationToken);
    Task ReportErrorAsync(AiErrorReport report, CancellationToken cancellationToken);
}
