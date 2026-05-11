namespace Page.Ui.Worker.Ai.Models;

public sealed class AiErrorReport
{
    public Guid ChatId { get; init; }
    public string ChatKey { get; init; } = string.Empty;
    public Guid VersionId { get; init; }
    public Guid? TriggerMessageId { get; init; }
    public string? TriggerMessageKey { get; init; }
    public string UserId { get; init; } = string.Empty;
    public List<string> Errors { get; init; } = new();
    public List<string> Logs { get; init; } = new();
    public List<AiErrorReportSourceFile> SourceFiles { get; init; } = new();
}

public sealed class AiErrorReportSourceFile
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string ObjectKey { get; init; } = string.Empty;
    public string? Content { get; init; }
    public string? LoadError { get; init; }
}
