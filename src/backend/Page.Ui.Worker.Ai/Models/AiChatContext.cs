using Page.Ui.Domain.Chat.Entities;

namespace Page.Ui.Worker.Ai.Models;

public sealed class AiChatContext
{
    public Chat Chat { get; init; } = null!;
    public Message TriggerMessage { get; init; } = null!;
    public IReadOnlyList<Message> History { get; init; } = Array.Empty<Message>();
}
