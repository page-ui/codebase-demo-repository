using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Page.Ui.Application.Chat.Inputs;
using Page.Ui.Application.Chat.Services;
using Page.Ui.Application.Common.Interfaces;
using Page.Ui.Presentation.Chat.GraphQl.Inputs;
using Page.Ui.Presentation.Chat.GraphQl.Views;
using Page.Ui.Presentation.Common.Security;
using System.Security.Claims;

namespace Page.Ui.Presentation.Chat.GraphQl.Mutations;

[ExtendObjectType("Mutation")]
public sealed class ChatMutations
{
    [Authorize(Policy = "UserApiPolicy")]
    public async Task<CreateChatPayloadView> CreateChat(
        CreateChatInput input,
        [Service] IChatService chatService,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId(currentUser);
        var payload = await chatService.CreateChatAsync(input, userId, cancellationToken);
        return ChatGraphQlMapper.ToView(payload, userId);
    }

    [Authorize(Policy = "AiApiPolicy")]
    public async Task<MessageView> CreateMessage(
        PublicCreateMessageInput input,
        [Service] IChatService chatService,
        [Service] IApplicationDbContext dbContext,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId(currentUser);
        var applicationInput = input.ToApplicationInput();
        if (currentUser.IsInternalAiPrincipal())
        {
            await ValidateInternalAiCreateMessageAsync(dbContext, currentUser, userId, applicationInput, cancellationToken);
        }

        var message = await chatService.CreateMessageAsync(applicationInput, userId, cancellationToken);
        return ChatGraphQlMapper.ToView(message, input.ChatKey, input.ReplyToKey, userId);
    }

    [Authorize(Policy = "AiApiPolicy")]
    public async Task<ChatView> RenameChat(
        RenameChatInput input,
        [Service] IChatService chatService,
        [Service] IApplicationDbContext dbContext,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId(currentUser);
        if (currentUser.IsInternalAiPrincipal())
        {
            await ValidateInternalAiChatAsync(dbContext, currentUser, userId, input.ChatKey, cancellationToken);
        }

        var chat = await chatService.RenameChatAsync(input.ChatKey, input.Name, userId, cancellationToken);
        return ChatGraphQlMapper.ToView(chat);
    }

    [Authorize(Policy = "UserApiPolicy")]
    public async Task<bool> DeleteChat(
        string chatKey,
        [Service] IChatService chatService,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId(currentUser);
        return await chatService.DeleteChatAsync(chatKey, userId, cancellationToken);
    }

    [Authorize(Policy = "UserApiPolicy")]
    public async Task<bool> ReportRenderError(
        ReportRenderErrorInput input,
        [Service] IChatService chatService,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredUserId(currentUser);
        return await chatService.ReportRenderErrorAsync(input, userId, cancellationToken);
    }

    private static string GetRequiredUserId(ClaimsPrincipal currentUser)
    {
        return currentUser.GetCurrentUserId()
            ?? throw new UnauthorizedAccessException("Authenticated user identifier was not found in token claims.");
    }

    private static async Task ValidateInternalAiCreateMessageAsync(
        IApplicationDbContext dbContext,
        ClaimsPrincipal currentUser,
        string userId,
        Page.Ui.Application.Chat.Inputs.CreateMessageInput input,
        CancellationToken cancellationToken)
    {
        var claimedMessageId = currentUser.GetInternalMessageId()
            ?? throw new UnauthorizedAccessException("Internal AI token is missing message_id.");

        if (string.IsNullOrWhiteSpace(input.ReplyToKey))
        {
            throw new UnauthorizedAccessException("Internal AI messages must reply to the triggering user message.");
        }

        var chat = await ValidateInternalAiChatAsync(dbContext, currentUser, userId, input.ChatKey, cancellationToken);
        var replyToMessage = await dbContext.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MessageKey == input.ReplyToKey && m.ChatId == chat.Id, cancellationToken);

        if (replyToMessage is null || replyToMessage.Id != claimedMessageId)
        {
            throw new UnauthorizedAccessException("Internal AI reply target does not match the triggering user message.");
        }
    }

    private static async Task<Page.Ui.Domain.Chat.Entities.Chat> ValidateInternalAiChatAsync(
        IApplicationDbContext dbContext,
        ClaimsPrincipal currentUser,
        string userId,
        string chatKey,
        CancellationToken cancellationToken)
    {
        var claimedChatId = currentUser.GetInternalChatId()
            ?? throw new UnauthorizedAccessException("Internal AI token is missing chat_id.");

        var chat = await dbContext.Chats
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ChatKey == chatKey && c.OwnerUserId == userId, cancellationToken);

        if (chat is null || chat.Id != claimedChatId)
        {
            throw new UnauthorizedAccessException("Internal AI token does not match the target chat.");
        }

        return chat;
    }
}
