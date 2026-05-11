using Page.Ui.Application.Chat.Inputs;
using Page.Ui.Application.Chat.Payloads;
using Page.Ui.Domain.Chat.Entities;

namespace Page.Ui.Application.Chat.Services;

public interface IChatService
{
    Task<CreateChatPayload> CreateChatAsync(CreateChatInput input, string userId, CancellationToken cancellationToken);
    Task<Message> CreateMessageAsync(CreateMessageInput input, string userId, CancellationToken cancellationToken);
    Task<Page.Ui.Domain.Chat.Entities.Chat?> GetChatAsync(string chatKey, string userId, CancellationToken cancellationToken);
    IQueryable<Page.Ui.Domain.Chat.Entities.Chat> GetChats(string userId);
    IQueryable<Page.Ui.Domain.Chat.Entities.Chat> SearchChats(string nameQuery, string userId);
    IQueryable<Message> GetMessages(string chatKey, string userId);
    IQueryable<Message> SearchMessages(string query, string? chatKey, string userId);
    Task<Page.Ui.Domain.Chat.Entities.Chat> RenameChatAsync(string chatKey, string name, string userId, CancellationToken cancellationToken);
    Task<bool> DeleteChatAsync(string chatKey, string userId, CancellationToken cancellationToken);
    Task<bool> ReportRenderErrorAsync(ReportRenderErrorInput input, string userId, CancellationToken cancellationToken);
}
