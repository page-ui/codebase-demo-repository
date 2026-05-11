namespace Page.Ui.Worker.Ai.Models;

public sealed class AiModelResult
{
    public string? Title { get; init; }
    public string? AssistantMessage { get; init; }
    public bool IsQuestion { get; init; }
    public bool ShouldGenerateUi { get; init; } = true;
    public IReadOnlyList<AiSourceFile> Files { get; init; } = Array.Empty<AiSourceFile>();
}
