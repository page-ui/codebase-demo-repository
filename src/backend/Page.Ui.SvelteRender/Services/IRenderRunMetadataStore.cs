using Page.Ui.Domain.Chat.Entities;
using Page.Ui.Domain.Chat.Enums;
using Page.Ui.SvelteRender.Models;

namespace Page.Ui.SvelteRender.Services;

public interface IRenderRunMetadataStore
{
    Task RecordAsync(RenderRequest request, RenderResponse response, string relativeRunPath, string? errorSummary, RenderRunStatus status, CancellationToken cancellationToken);
    Task<RenderRun?> GetByRunIdAsync(string runId, CancellationToken cancellationToken);
    Task<RenderRun?> GetByPublicRunTokenAsync(string publicRunToken, CancellationToken cancellationToken);
    Task<RenderRun?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RenderRun>> GetByUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<RenderRun>> GetByChatIdAsync(Guid chatId, int page, int pageSize, CancellationToken cancellationToken);
    Task MarkPrunedAsync(string runId, CancellationToken cancellationToken);
}
