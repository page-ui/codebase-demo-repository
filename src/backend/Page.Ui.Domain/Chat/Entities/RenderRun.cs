using Page.Ui.Domain.Chat.Enums;

namespace Page.Ui.Domain.Chat.Entities;

public class RenderRun
{
    public string RunId { get; set; } = string.Empty;
    public string PublicRunToken { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserStorageKey { get; set; }
    public Guid? ChatId { get; set; }
    public string? ChatKey { get; set; }
    public Guid? MessageId { get; set; }
    public Guid? VersionId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastAccessedAtUtc { get; set; }
    public RenderRunStatus Status { get; set; } = RenderRunStatus.Succeeded;
    public string RelativeRunPath { get; set; } = string.Empty;
    public string? PreviewUrl { get; set; }
    public string? ErrorSummary { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public string? ContentHash { get; set; }
    public string? SourceHash { get; set; }
}
