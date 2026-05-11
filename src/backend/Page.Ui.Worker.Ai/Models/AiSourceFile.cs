namespace Page.Ui.Worker.Ai.Models;

public sealed class AiSourceFile
{
    public string FileName { get; init; } = string.Empty;
    public string? Content { get; init; }
    public string? ObjectKey { get; init; }
    public string ContentType { get; init; } = "text/plain";
}
