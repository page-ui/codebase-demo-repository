using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Page.Ui.Application.Chat.Contracts;
using Page.Ui.Infrastructure.Auth.Persistence;
using Page.Ui.Presentation.Common.Security;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;

namespace Page.Ui.Presentation.Chat.Controllers;

[Authorize(Policy = "InternalAiApiPolicy")]
[ApiController]
[Route("api/ai-dev")]
public class AiDevUploadController : ControllerBase
{
    private const int PresignRequestsPerUserWindow = 40;
    private const int PresignRequestsPerIpWindow = 80;
    private const int MaxOriginalFileNameLength = 260;
    private static readonly TimeSpan PresignWindow = TimeSpan.FromMinutes(1);
    private static readonly SemaphoreSlim BucketEnsureLock = new(1, 1);
    private static volatile bool _bucketConfirmed;

    private readonly IMinioClient _minioClient;
    private readonly IConnectionMultiplexer _redis;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AiDevUploadController> _logger;
    private const string BucketName = "ai-runs";

    public AiDevUploadController(
        IMinioClient minioClient,
        IConnectionMultiplexer redis,
        IPublishEndpoint publishEndpoint,
        ApplicationDbContext dbContext,
        ILogger<AiDevUploadController> logger)
    {
        _minioClient = minioClient;
        _redis = redis;
        _publishEndpoint = publishEndpoint;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost("render-trigger")]
    public async Task<IActionResult> TriggerRender([FromBody] RenderTriggerInput input)
    {
        if (input == null || input.Files == null || input.Files.Count == 0)
        {
            return BadRequest("Files are required.");
        }

        if (input.RunId == Guid.Empty || input.VersionId == Guid.Empty)
        {
            return BadRequest("runId and versionId must be non-empty GUIDs.");
        }

        var currentUserId = User.GetCurrentUserId();
        var claimedChatId = User.GetInternalChatId();
        var claimedMessageId = User.GetInternalMessageId();
        if (string.IsNullOrWhiteSpace(currentUserId) || claimedChatId is null || claimedMessageId is null)
        {
            return Forbid();
        }

        var chat = await _dbContext.Chats
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ChatKey == input.ChatKey && c.OwnerUserId == currentUserId, HttpContext.RequestAborted);
        if (chat is null || chat.Id != claimedChatId.Value)
        {
            return Forbid();
        }

        if (input.ChatId != chat.Id || input.ReplyToMessageId != claimedMessageId.Value)
        {
            return BadRequest("Chat or reply target does not match the authenticated AI token.");
        }

        var expectedUserStorageKey = BuildUserStorageKey(currentUserId);
        if (!string.Equals(input.UserStorageKey, expectedUserStorageKey, StringComparison.Ordinal))
        {
            return BadRequest("userStorageKey does not match the authenticated AI token.");
        }

        var expectedPrefix = BuildSourcePrefix(expectedUserStorageKey, chat.ChatKey, input.VersionId);
        if (input.Files.Any(file => !IsAllowedAiSourceFile(file, expectedPrefix)))
        {
            return BadRequest("One or more files are outside the allowed AI storage prefix.");
        }

        try
        {
            await _publishEndpoint.Publish(new TriggerAiRunRender(
                chat.Id,
                chat.ChatKey,
                claimedMessageId.Value,
                input.RunId,
                input.VersionId,
                expectedUserStorageKey,
                input.Files
            ));

            await _dbContext.SaveChangesAsync();

            return Accepted();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing render trigger for chat {ChatId}", input.ChatId);
            return StatusCode(500, "Internal server error");
        }
    }

    public record RenderTriggerInput(
        Guid ChatId,
        string ChatKey,
        Guid ReplyToMessageId,
        Guid RunId,
        Guid VersionId,
        string UserStorageKey,
        List<AiSourceFileDto> Files
    );

    [HttpGet("upload/presign")]
    public async Task<IActionResult> GetPresignedUrl(
        [FromQuery] string userStorageKey,
        [FromQuery] string chatKey,
        [FromQuery] string versionId,
        [FromQuery] string fileName)
    {
        try
        {
            var safeFileName = NormalizeOriginalFileName(fileName);
            if (safeFileName is null)
            {
                return BadRequest("fileName is required and must be a valid file name.");
            }

            if (string.IsNullOrWhiteSpace(userStorageKey) || string.IsNullOrWhiteSpace(chatKey) || string.IsNullOrWhiteSpace(versionId))
            {
                return BadRequest("userStorageKey, chatKey, and versionId are required.");
            }

            if (!Guid.TryParse(versionId, out var parsedVersionId) || parsedVersionId == Guid.Empty)
            {
                return BadRequest("versionId must be a non-empty GUID.");
            }

            var userId = User.GetCurrentUserId();
            var claimedChatId = User.GetInternalChatId();
            if (string.IsNullOrWhiteSpace(userId) || claimedChatId is null)
            {
                return Forbid();
            }

            var chat = await _dbContext.Chats
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ChatKey == chatKey && c.OwnerUserId == userId, HttpContext.RequestAborted);
            if (chat is null || chat.Id != claimedChatId.Value)
            {
                return Forbid();
            }

            var expectedUserStorageKey = BuildUserStorageKey(userId);
            if (!string.Equals(userStorageKey, expectedUserStorageKey, StringComparison.Ordinal))
            {
                return BadRequest("userStorageKey does not match the authenticated AI token.");
            }

            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (await IsRateLimitedAsync($"ratelimit:aidev:presign:user:{NormalizeRateLimitPart(userId)}", PresignRequestsPerUserWindow, PresignWindow) ||
                await IsRateLimitedAsync($"ratelimit:aidev:presign:ip:{NormalizeRateLimitPart(remoteIp)}", PresignRequestsPerIpWindow, PresignWindow))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, "Too many upload URL requests. Please slow down.");
            }

            await EnsureBucketExistsAsync();
            var objectKey = $"{BuildSourcePrefix(expectedUserStorageKey, chat.ChatKey, parsedVersionId)}{safeFileName}";

            var putArgs = new PresignedPutObjectArgs()
                .WithBucket(BucketName)
                .WithObject(objectKey)
                .WithExpiry(600);

            var uploadUrl = await _minioClient.PresignedPutObjectAsync(putArgs);

            var getArgs = new PresignedGetObjectArgs()
                .WithBucket(BucketName)
                .WithObject(objectKey)
                .WithExpiry(3600 * 24);

            var downloadUrl = await _minioClient.PresignedGetObjectAsync(getArgs);

            return Ok(new
            {
                uploadUrl = RewriteToClientAccessibleUrl(uploadUrl),
                downloadUrl = RewriteToClientAccessibleUrl(downloadUrl),
                objectKey
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI dev presigned URL");
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

    private static string BuildUserStorageKey(string userId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(userId.Trim().ToLowerInvariant()));
        return Convert.ToHexString(hash)[..24].ToLowerInvariant();
    }

    private static string BuildSourcePrefix(string userStorageKey, string chatKey, Guid versionId)
    {
        return $"users/{userStorageKey}/chats/{chatKey}/versions/{versionId:D}/source/";
    }

    private static bool IsAllowedAiSourceFile(AiSourceFileDto file, string expectedPrefix)
    {
        if (string.IsNullOrWhiteSpace(file.FileName) ||
            string.IsNullOrWhiteSpace(file.ObjectKey) ||
            string.IsNullOrWhiteSpace(file.ContentType))
        {
            return false;
        }

        var normalizedFileName = System.IO.Path.GetFileName(file.FileName.Trim().Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(normalizedFileName))
        {
            return false;
        }

        return file.ObjectKey.StartsWith(expectedPrefix, StringComparison.Ordinal) &&
               string.Equals(System.IO.Path.GetFileName(file.ObjectKey), normalizedFileName, StringComparison.Ordinal);
    }
}
