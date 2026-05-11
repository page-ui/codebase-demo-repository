using Page.Ui.Domain.Chat.Entities;
using Page.Ui.Domain.Chat.Enums;
using Page.Ui.SvelteRender.Models;

namespace Page.Ui.SvelteRender.Services;

public sealed class NullRenderRunMetadataStore : IRenderRunMetadataStore
{
    public Task RecordAsync(RenderRequest request, RenderResponse response, string relativeRunPath, string? errorSummary, RenderRunStatus status, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<RenderRun?> GetByRunIdAsync(string runId, CancellationToken cancellationToken)
        => Task.FromResult<RenderRun?>(null);

    public Task<RenderRun?> GetByPublicRunTokenAsync(string publicRunToken, CancellationToken cancellationToken)
        => Task.FromResult<RenderRun?>(null);

    public Task<RenderRun?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken)
        => Task.FromResult<RenderRun?>(null);

    public Task<IReadOnlyList<RenderRun>> GetByUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<RenderRun>>(Array.Empty<RenderRun>());

    public Task<IReadOnlyList<RenderRun>> GetByChatIdAsync(Guid chatId, int page, int pageSize, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<RenderRun>>(Array.Empty<RenderRun>());

    public Task MarkPrunedAsync(string runId, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
