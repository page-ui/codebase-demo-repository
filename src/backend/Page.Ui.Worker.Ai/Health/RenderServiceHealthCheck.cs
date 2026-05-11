using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Page.Ui.Worker.Ai.Health;

public sealed class RenderServiceHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RenderServiceHealthCheck> _logger;

    public RenderServiceHealthCheck(IHttpClientFactory httpClientFactory, ILogger<RenderServiceHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("SvelteRender");
            using var response = await client.GetAsync("health", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Render service is reachable.");
            }

            _logger.LogWarning("Render service health check returned non-success status code {StatusCode}.", (int)response.StatusCode);
            return HealthCheckResult.Unhealthy($"Render service returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Render service health check failed.");
            return HealthCheckResult.Unhealthy("Render service is not reachable.", ex);
        }
    }
}
