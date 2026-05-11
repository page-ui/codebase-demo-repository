using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Page.Ui.Application.Common.Interfaces;
using Page.Ui.Presentation.Chat.GraphQl.Views;
using Page.Ui.Presentation.Common.Security;
using System.Security.Claims;

namespace Page.Ui.Presentation.Chat.GraphQl.Subscriptions;

[ExtendObjectType("Subscription")]
public sealed class ChatSubscriptions
{
    private static readonly TimeSpan ChatAccessCacheDuration = TimeSpan.FromMinutes(2);

    [Authorize(Policy = "UserApiPolicy")]
    [Subscribe]
    [Topic("OnMessageCreated_{chatKey}")]
    public async Task<MessageView> OnMessageCreated(
        string chatKey,
        ClaimsPrincipal currentUser,
        [Service] IApplicationDbContext context,
        [Service] IMemoryCache cache,
        [EventMessage] MessageView message,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new GraphQLException("Unauthorized");
        }

        var cacheKey = $"chat-subscription-access:{chatKey}:{userId}";
        if (!cache.TryGetValue(cacheKey, out bool hasAccess))
        {
            hasAccess = await context.Chats
                .AnyAsync(c => c.ChatKey == chatKey && c.OwnerUserId == userId, cancellationToken);

            cache.Set(cacheKey, hasAccess, ChatAccessCacheDuration);
        }

        if (!hasAccess)
        {
            throw new GraphQLException("Forbidden");
        }

        return message;
    }
}
