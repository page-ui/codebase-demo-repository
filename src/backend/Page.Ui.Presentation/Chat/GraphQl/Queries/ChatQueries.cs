using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using HotChocolate.Types;
using Page.Ui.Application.Chat.Services;
using Page.Ui.Domain.Chat.Entities;
using Page.Ui.Presentation.Chat.GraphQl.Views;
using Page.Ui.Presentation.Common.Security;
using System.Security.Claims;

namespace Page.Ui.Presentation.Chat.GraphQl.Queries;

[ExtendObjectType("Query")]
public sealed class ChatQueries
{
    private const int DefaultConnectionPageSize = 20;
    private const int MaxConnectionPageSize = 50;

    [Authorize(Policy = "UserApiPolicy")]
    [UseFirstOrDefault]
    public IQueryable<ChatView> GetChat(
        string chatKey,
        [Service] IChatService chatService,
        ClaimsPrincipal currentUser)
    {
        var userId = GetRequiredUserId(currentUser);
        return chatService.GetChats(userId)
            .Where(chat => chat.ChatKey == chatKey)
            .Select(ChatGraphQlMapper.ProjectChat());
    }

    [Authorize(Policy = "UserApiPolicy")]
    [UsePaging(IncludeTotalCount = true, DefaultPageSize = DefaultConnectionPageSize, MaxPageSize = MaxConnectionPageSize)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ChatView> GetChats(
        [Service] IChatService chatService,
        ClaimsPrincipal currentUser)
    {
        return chatService.GetChats(GetRequiredUserId(currentUser))
            .Select(ChatGraphQlMapper.ProjectChat());
    }

    [Authorize(Policy = "UserApiPolicy")]
    [UsePaging(IncludeTotalCount = true, DefaultPageSize = DefaultConnectionPageSize, MaxPageSize = MaxConnectionPageSize)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ChatView> SearchChats(
        string name,
        [Service] IChatService chatService,
        ClaimsPrincipal currentUser)
    {
        return chatService.SearchChats(name, GetRequiredUserId(currentUser))
            .Select(ChatGraphQlMapper.ProjectChat());
    }

    [Authorize(Policy = "UserApiPolicy")]
    [UsePaging(IncludeTotalCount = true, DefaultPageSize = DefaultConnectionPageSize, MaxPageSize = MaxConnectionPageSize)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<MessageView> GetMessages(
        string chatKey,
        [Service] IChatService chatService,
        ClaimsPrincipal currentUser)
    {
        var userId = GetRequiredUserId(currentUser);
        return chatService.GetMessages(chatKey, userId)
            .Select(ChatGraphQlMapper.ProjectMessage(userId));
    }

    [Authorize(Policy = "UserApiPolicy")]
    [UsePaging(IncludeTotalCount = true, DefaultPageSize = DefaultConnectionPageSize, MaxPageSize = MaxConnectionPageSize)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<MessageView> SearchMessages(
        string query,
        string? chatKey,
        [Service] IChatService chatService,
        ClaimsPrincipal currentUser)
    {
        var userId = GetRequiredUserId(currentUser);
        return chatService.SearchMessages(query, chatKey, userId)
            .Select(ChatGraphQlMapper.ProjectMessage(userId));
    }

    private static string GetRequiredUserId(ClaimsPrincipal currentUser)
    {
        return currentUser.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Authenticated user identifier was not found in token claims.");
    }
}
