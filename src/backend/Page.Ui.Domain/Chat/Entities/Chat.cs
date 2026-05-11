using Page.Ui.Domain.Auth.Entities;
using Page.Ui.Domain.Chat;

namespace Page.Ui.Domain.Chat.Entities;

public class Chat
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerUserId { get; set; } = null!;
    public virtual ApplicationUser OwnerUser { get; set; } = null!;
    public string? Name { get; set; }
    public string ChatKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = ChatConstants.AiModelId;
    public string? SystemPrompt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
