using StackExchange.Redis;

namespace Page.Ui.Presentation.Auth.GraphQl.Support;

internal static class AuthRateLimitGuard
{
    public static string NormalizePart(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "unknown";
        }

        return input.Trim().ToLowerInvariant().Replace(':', '_');
    }

    public static async Task<bool> IsRateLimitedAsync(IConnectionMultiplexer redis, string key, int maxRequests, TimeSpan window)
    {
        var db = redis.GetDatabase();
        var count = await db.StringIncrementAsync(key);
        if (count == 1)
        {
            await db.KeyExpireAsync(key, window);
        }

        return count > maxRequests;
    }
}
