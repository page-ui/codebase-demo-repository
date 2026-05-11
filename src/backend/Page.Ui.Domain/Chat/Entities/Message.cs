using Page.Ui.Domain.Auth.Entities;
using Page.Ui.Domain.Chat.Enums;

namespace Page.Ui.Domain.Chat.Entities;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ChatId { get; set; }
    public virtual Chat Chat { get; set; } = null!;

    public string SenderId { get; set; } = null!;
    public virtual ApplicationUser Sender { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsQuestion { get; set; } = false;
    public MessageType Type { get; set; } = MessageType.Text;
    public MessageStatus Status { get; set; } = MessageStatus.Sent;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    public Guid? ReplyToId { get; set; }
    public virtual Message? ReplyTo { get; set; }

    public string? AttachmentUrl { get; set; }
    public string? ClientRequestId { get; set; }
    public string? ServerGeneratedId { get; set; }
    public string MessageKey { get; set; } = string.Empty;

}
