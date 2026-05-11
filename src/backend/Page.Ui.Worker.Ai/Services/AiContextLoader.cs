using Microsoft.EntityFrameworkCore;
using Page.Ui.Application.Chat.Contracts;
using Page.Ui.Infrastructure.Auth.Persistence;
using Page.Ui.Worker.Ai.Models;

namespace Page.Ui.Worker.Ai.Services;

public sealed class AiContextLoader : IAiContextLoader
{
    private readonly ApplicationDbContext _dbContext;

    public AiContextLoader(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AiChatContext?> LoadAsync(ChatMessageCreated message, CancellationToken cancellationToken)
    {
        var chat = await _dbContext.Chats
            .AsTracking()
            .FirstOrDefaultAsync(c => c.Id == message.ChatId, cancellationToken);

        if (chat is null)
        {
            return null;
        }

        var triggerMessage = await _dbContext.Messages
            .AsTracking()
            .FirstOrDefaultAsync(m => m.Id == message.Id, cancellationToken);

        if (triggerMessage is null)
        {
            return null;
        }

        var history = await _dbContext.Messages
            .AsNoTracking()
            .Where(m => m.ChatId == message.ChatId)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        return new AiChatContext
        {
            Chat = chat,
            TriggerMessage = triggerMessage,
            History = history
        };
    }
}
