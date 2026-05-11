using Page.Ui.Domain.Chat.Entities;

namespace Page.Ui.Worker.Ai.Models;

public sealed class StoredAiRun
{
    public AiRun Run { get; init; } = null!;
    public IReadOnlyList<AiRunFile> Files { get; init; } = Array.Empty<AiRunFile>();
    public string UserStorageKey { get; init; } = string.Empty;
}
