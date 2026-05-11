using Page.Ui.Domain.Chat.Enums;

namespace Page.Ui.Domain.Chat.Entities;

public class AiRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VersionId { get; set; } = Guid.NewGuid();

    public Guid ChatId { get; set; }
    public virtual Chat Chat { get; set; } = null!;

    public string OwnerUserId { get; set; } = string.Empty;

    public Guid? TriggerMessageId { get; set; }
    public virtual Message? TriggerMessage { get; set; }

    public string ModelId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    public Guid? SupersededByRunId { get; set; }
    public virtual AiRun? SupersededByRun { get; set; }

    public AiRunStatus Status { get; set; } = AiRunStatus.Accepted;
    public string ManifestObjectKey { get; set; } = string.Empty;
    public string? FinalPreviewUrl { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessageSafe { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public string? ClientErrors { get; set; }
    public string? ClientLogs { get; set; }

    public virtual ICollection<AiRunFile> Files { get; set; } = new List<AiRunFile>();
}
