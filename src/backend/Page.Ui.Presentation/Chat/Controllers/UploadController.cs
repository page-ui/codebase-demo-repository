using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using StackExchange.Redis;
using System.Security.Claims;

namespace Page.Ui.Presentation.Chat.Controllers;

[Authorize(Policy = "UserApiPolicy")]
[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private const int PresignRequestsPerUserWindow = 20;
    private const int PresignRequestsPerIpWindow = 40;
    private const int MaxOriginalFileNameLength = 180;
    private static readonly TimeSpan PresignWindow = TimeSpan.FromMinutes(1);
    private static readonly SemaphoreSlim BucketEnsureLock = new(1, 1);
    private static volatile bool _bucketConfirmed;

    private readonly IMinioClient _minioClient;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<UploadController> _logger;
    private const string BucketName = "chat-uploads";

    public UploadController(IMinioClient minioClient, IConnectionMultiplexer redis, ILogger<UploadController> logger)
    {
        _minioClient = minioClient;
        _redis = redis;
        _logger = logger;
    }

    [HttpGet("presign")]
    public async Task<IActionResult> GetPresignedUrl([FromQuery] string fileName)
    {
        try
        {
            var safeFileName = NormalizeOriginalFileName(fileName);
            if (safeFileName is null)
            {
                return BadRequest("fileName is required and must be a valid file name.");
            }

            var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (await IsRateLimitedAsync($"ratelimit:upload:presign:user:{NormalizeRateLimitPart(userId)}", PresignRequestsPerUserWindow, PresignWindow) ||
                await IsRateLimitedAsync($"ratelimit:upload:presign:ip:{NormalizeRateLimitPart(remoteIp)}", PresignRequestsPerIpWindow, PresignWindow))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, "Too many upload URL requests. Please slow down.");
            }

            await EnsureBucketExistsAsync();
            var objectName = $"{userId}/{Guid.NewGuid():N}_{safeFileName}";

            var putArgs = new PresignedPutObjectArgs()
                .WithBucket(BucketName)
                .WithObject(objectName)
                .WithExpiry(600);

            var uploadUrl = await _minioClient.PresignedPutObjectAsync(putArgs);

            var getArgs = new PresignedGetObjectArgs()
                .WithBucket(BucketName)
                .WithObject(objectName)
                .WithExpiry(3600 * 24);

            var downloadUrl = await _minioClient.PresignedGetObjectAsync(getArgs);

            return Ok(new { 
                uploadUrl = RewriteToClientAccessibleUrl(uploadUrl), 
                downloadUrl = RewriteToClientAccessibleUrl(downloadUrl), 
                fileName = objectName 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL");
            return StatusCode(500, "Internal server error");
        }
    }

    private async Task<bool> IsRateLimitedAsync(string key, int maxRequests, TimeSpan window)
    {
        var db = _redis.GetDatabase();
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

    private async Task EnsureBucketExistsAsync()
    {
        if (_bucketConfirmed) return;

        await BucketEnsureLock.WaitAsync();
        try
        {
            if (_bucketConfirmed) return;

            var exists = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(BucketName));
            if (exists)
            {
                _bucketConfirmed = true;
                return;
            }

            try
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(BucketName));
                _bucketConfirmed = true;
            }
            catch (MinioException)
            {
                var createdByAnotherRequest = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(BucketName));
                if (!createdByAnotherRequest) throw;
                _bucketConfirmed = true;
            }
        }
        finally
        {
            BucketEnsureLock.Release();
        }
    }

    private static string? NormalizeOriginalFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;

        var trimmed = fileName.Trim().Replace('\\', '/');
        var name = System.IO.Path.GetFileName(trimmed);
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaxOriginalFileNameLength) return null;
        if (name.Any(char.IsControl)) return null;

        return name;
    }

    private string RewriteToClientAccessibleUrl(string presignedUrl)
    {
        if (!Uri.TryCreate(presignedUrl, UriKind.Absolute, out var uri)) return presignedUrl;
        return $"/minio{uri.AbsolutePath}{uri.Query}";
    }
}
