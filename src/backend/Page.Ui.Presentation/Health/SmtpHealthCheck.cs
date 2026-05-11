using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Page.Ui.Presentation.Health;

public sealed class SmtpHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpHealthCheck> _logger;

    public SmtpHealthCheck(IConfiguration configuration, ILogger<SmtpHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var host = _configuration["Smtp:Host"] ?? _configuration["Smtp__Host"];
        var portValue = _configuration["Smtp:Port"] ?? _configuration["Smtp__Port"];

        if (string.IsNullOrWhiteSpace(host) || !int.TryParse(portValue, out var port))
        {
            _logger.LogWarning("SMTP health check failed because host/port configuration is missing.");
            return HealthCheckResult.Unhealthy("SMTP host/port configuration is missing.");
        }

        try
        {
            using var client = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));
            await client.ConnectAsync(host, port, timeoutCts.Token);
            return HealthCheckResult.Healthy("SMTP endpoint is reachable.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMTP health check failed for {Host}:{Port}.", host, port);
            return HealthCheckResult.Unhealthy("SMTP endpoint is not reachable.", ex);
        }
    }
}
