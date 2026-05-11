namespace Page.Ui.Domain.Chat.Entities;

public class AiRunFile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RunId { get; set; }
    public virtual AiRun Run { get; set; } = null!;

    public string ObjectKey { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string StoredFileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
