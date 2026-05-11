using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Page.Ui.Presentation.Common.Security;
using StackExchange.Redis;

namespace Page.Ui.Presentation.Chat.GraphQl;

public sealed class ChatSocketSessionInterceptor : ISocketSessionInterceptor
{
    private const int ConnectionInitRequestsPerWindow = 40;
    private static readonly TimeSpan ConnectionInitWindow = TimeSpan.FromSeconds(30);

    private readonly ILogger<ChatSocketSessionInterceptor> _logger;
    private readonly IConnectionMultiplexer _redis;

    public ChatSocketSessionInterceptor(ILogger<ChatSocketSessionInterceptor> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
    }

    public async ValueTask<ConnectionStatus> OnConnectAsync(
        ISocketSession session,
        IOperationMessagePayload connectionInitMessage,
        CancellationToken cancellationToken)
    {
        var httpContext = session.Connection.HttpContext;

        if (await IsConnectionRateLimitedAsync(httpContext))
        {
            _logger.LogWarning(
                "GraphQL WS connection rejected due to rate limiting. RemoteIp={RemoteIp}",
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            return ConnectionStatus.Reject("Too many connection attempts. Please slow down.");
        }

        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            return ConnectionStatus.Accept();
        }

        var initialAuthResult = await httpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (initialAuthResult.Succeeded && initialAuthResult.Principal is not null)
        {
            httpContext.User = initialAuthResult.Principal;
            return ConnectionStatus.Accept();
        }

        var hasPayloadToken = WebSocketAccessTokenReader.TryRead(connectionInitMessage, out var token);
        var hasQueryToken = false;
        if (!hasPayloadToken)
        {
            hasQueryToken = WebSocketAccessTokenReader.TryRead(httpContext.Request.Query, out token);
        }

        if (!hasPayloadToken && !hasQueryToken)
        {
            var queryKeys = string.Join(",", httpContext.Request.Query.Keys);
            _logger.LogWarning(
                "GraphQL WS connection rejected: no access_token found in connection_init payload or query string. QueryKeys={QueryKeys}",
                queryKeys);
            return ConnectionStatus.Reject("Unauthorized");
        }

        token = WebSocketAccessTokenReader.Normalize(token);
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("GraphQL WS connection rejected: access_token was empty after normalization.");
            return ConnectionStatus.Reject("Unauthorized");
        }

        if (!token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = $"Bearer {token}";
        }

        httpContext.Request.Headers.Authorization = token;

        var authResult = await httpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (authResult.Succeeded && authResult.Principal is not null)
        {
            httpContext.User = authResult.Principal;
            return ConnectionStatus.Accept();
        }

        _logger.LogWarning(
            "GraphQL WS connection rejected: token authentication failed. Failure={Failure}",
            authResult.Failure?.Message ?? "none");
        return ConnectionStatus.Reject("Unauthorized");
    }

    public ValueTask OnRequestAsync(
        ISocketSession session,
        string operationSessionId,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var user = session.Connection.HttpContext.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            requestBuilder.SetUser(user);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<IOperationResult> OnResultAsync(
        ISocketSession session,
        string operationSessionId,
        IOperationResult result,
        CancellationToken cancellationToken)
        => new(result);

    public ValueTask OnCompleteAsync(
        ISocketSession session,
        string operationSessionId,
        CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    public ValueTask<IReadOnlyDictionary<string, object?>?> OnPingAsync(
        ISocketSession session,
        IOperationMessagePayload pingMessage,
        CancellationToken cancellationToken)
        => new((IReadOnlyDictionary<string, object?>?)null);

    public ValueTask OnPongAsync(
        ISocketSession session,
        IOperationMessagePayload pongMessage,
        CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    public ValueTask OnCloseAsync(
        ISocketSession session,
        CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    private async ValueTask<bool> IsConnectionRateLimitedAsync(HttpContext httpContext)
    {
        var remoteIp = NormalizeRateLimitPart(httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        var userId = NormalizeRateLimitPart(httpContext.User.GetCurrentUserId() ?? "anonymous");
        var db = _redis.GetDatabase();

        if (await IsRateLimitedAsync(db, $"ratelimit:gqlws:connect:ip:{remoteIp}", ConnectionInitRequestsPerWindow, ConnectionInitWindow))
        {
            return true;
        }

        if (userId != "anonymous" &&
            await IsRateLimitedAsync(db, $"ratelimit:gqlws:connect:user:{userId}", ConnectionInitRequestsPerWindow, ConnectionInitWindow))
        {
            return true;
        }

        return false;
    }

    private static async Task<bool> IsRateLimitedAsync(IDatabase db, string key, int maxRequests, TimeSpan window)
    {
        var count = await db.StringIncrementAsync(key);
        if (count == 1)
        {
            await db.KeyExpireAsync(key, window);
        }

        return count > maxRequests;
    }

    private static string NormalizeRateLimitPart(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "unknown";
        }

        return input.Trim().ToLowerInvariant().Replace(':', '_');
    }
}
