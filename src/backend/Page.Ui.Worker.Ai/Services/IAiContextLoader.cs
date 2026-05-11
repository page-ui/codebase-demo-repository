using Page.Ui.Application.Chat.Contracts;
using Page.Ui.Worker.Ai.Models;

namespace Page.Ui.Worker.Ai.Services;

public interface IAiContextLoader
{
    Task<AiChatContext?> LoadAsync(ChatMessageCreated message, CancellationToken cancellationToken);
}
