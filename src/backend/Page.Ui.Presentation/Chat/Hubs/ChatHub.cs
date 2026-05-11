using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Page.Ui.Application.Common.Interfaces;
using Page.Ui.Presentation.Common.Security;
using StackExchange.Redis;

namespace Page.Ui.Presentation.Chat.Hubs;

[Authorize(Policy = "UserApiPolicy")]
public class ChatHub : Hub
{
    private const int JoinRequestsPerWindow = 30;
    private static readonly TimeSpan JoinWindow = TimeSpan.FromSeconds(10);

    private readonly IApplicationDbContext _context;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IApplicationDbContext context, IConnectionMultiplexer redis, ILogger<ChatHub> logger)
    {
        _context = context;
        _redis = redis;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        if (userId != null)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync($"presence:user:{userId}", "online", TimeSpan.FromMinutes(5));
            _logger.LogInformation("User {UserId} connected to ChatHub", userId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        if (userId != null)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync($"presence:user:{userId}");
            _logger.LogInformation("User {UserId} disconnected from ChatHub", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinChat(string chatKey)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new HubException("Unauthorized");
        }

        await EnforceJoinRateLimit(userId);

        if (string.IsNullOrWhiteSpace(chatKey))
        {
            throw new HubException("Invalid chat key");
        }

        var hasAccess = await _context.Chats
            .AnyAsync(c => c.ChatKey == chatKey && c.OwnerUserId == userId, Context.ConnectionAborted);

        if (!hasAccess)
        {
            _logger.LogWarning("User {UserId} attempted to join unauthorized chat {ChatKey}", userId, chatKey);
            throw new HubException("Forbidden");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, chatKey);
    }

    public async Task LeaveChat(string chatKey)
    {
        if (string.IsNullOrWhiteSpace(chatKey))
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatKey);
    }

    private async Task EnforceJoinRateLimit(string userId)
    {
        var db = _redis.GetDatabase();
        var key = $"ratelimit:chat:join:{userId}";
        var count = await db.StringIncrementAsync(key);

        if (count == 1)
        {
            await db.KeyExpireAsync(key, JoinWindow);
        }

        if (count > JoinRequestsPerWindow)
        {
            throw new HubException("Too many join requests. Please slow down.");
        }
    }

    private string? GetCurrentUserId()
    {
        return Context.User.GetCurrentUserId();
    }
}
