using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Page.Ui.SvelteRender.Models;

namespace Page.Ui.SvelteRender.Health;

public sealed class SandboxHealthCheck : IHealthCheck
{
    private readonly SandboxOptions _sandboxOptions;
    private readonly ILogger<SandboxHealthCheck> _logger;

    public SandboxHealthCheck(IOptions<SandboxOptions> sandboxOptions, ILogger<SandboxHealthCheck> logger)
    {
        _sandboxOptions = sandboxOptions.Value;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_sandboxOptions.Endpoint) ||
            !Uri.TryCreate(_sandboxOptions.Endpoint, UriKind.Absolute, out var endpoint))
        {
            _logger.LogWarning("Sandbox health check failed because endpoint configuration is invalid. Endpoint={Endpoint}", _sandboxOptions.Endpoint);
            return HealthCheckResult.Unhealthy("Sandbox endpoint configuration is invalid.");
        }

        var port = endpoint.Port > 0 ? endpoint.Port : 4000;

        try
        {
            using var client = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(3, Math.Min(_sandboxOptions.TimeoutSeconds, 10))));
            await client.ConnectAsync(endpoint.Host, port, timeoutCts.Token);
            return HealthCheckResult.Healthy("Sandbox is reachable.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sandbox health check failed for {Host}:{Port}.", endpoint.Host, port);
            return HealthCheckResult.Unhealthy("Sandbox is not reachable.", ex);
        }
    }
}
