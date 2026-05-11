using Page.Ui.Worker.Ai.Models;

namespace Page.Ui.Worker.Ai.Services;

public interface IAiRunStorageService
{
    Task<StoredAiRun> StoreAsync(AiChatContext context, AiModelResult result, Guid versionId, Guid runId, CancellationToken cancellationToken);
    Task<(string Html, string Css, string Js)> LoadRenderInputsAsync(StoredAiRun storedRun, CancellationToken cancellationToken);
    Task PromoteCurrentAsync(StoredAiRun storedRun, CancellationToken cancellationToken);
    Task MarkFailedAsync(StoredAiRun storedRun, string failureCode, string failureMessage, CancellationToken cancellationToken);
    Task<string> GetObjectContentAsync(string objectKey, CancellationToken cancellationToken);
}
