using Microsoft.Extensions.Diagnostics.HealthChecks;
using Minio;
using Minio.DataModel.Args;

namespace Page.Ui.Presentation.Health;

public sealed class MinioHealthCheck : IHealthCheck
{
    private const string BucketName = "chat-uploads";
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioHealthCheck> _logger;

    public MinioHealthCheck(IMinioClient minioClient, ILogger<MinioHealthCheck> logger)
    {
        _minioClient = minioClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(BucketName),
                cancellationToken);

            if (exists)
            {
                return HealthCheckResult.Healthy("MinIO bucket is reachable.");
            }

            _logger.LogWarning("MinIO health check failed because bucket {BucketName} was not found.", BucketName);
            return HealthCheckResult.Unhealthy("MinIO is reachable but the chat-uploads bucket is missing.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MinIO health check failed.");
            return HealthCheckResult.Unhealthy("MinIO check failed.", ex);
        }
    }
}
