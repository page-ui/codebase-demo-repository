using StackExchange.Redis;

namespace Page.Ui.Infrastructure.Chat.Services;

internal static class ChatRateLimitGuard
{
    public static void EnforceRead(IConnectionMultiplexer redis, string userId, string operation, int maxRequests, TimeSpan window)
    {
        Enforce(redis, $"ratelimit:chat:read:{operation}:{userId}", maxRequests, window, "Too many read requests. Please slow down.");
    }

    public static void EnforceWrite(IConnectionMultiplexer redis, string userId, string operation, int maxRequests, TimeSpan window)
    {
        Enforce(redis, $"ratelimit:chat:write:{operation}:{userId}", maxRequests, window, "Too many write requests. Please slow down.");
    }

    private static void Enforce(IConnectionMultiplexer redis, string key, int maxRequests, TimeSpan window, string errorMessage)
    {
        var db = redis.GetDatabase();
        var count = db.StringIncrement(key);

        if (count == 1)
        {
            db.KeyExpire(key, window);
        }

        if (count > maxRequests)
        {
            throw new InvalidOperationException(errorMessage);
        }
    }
}
