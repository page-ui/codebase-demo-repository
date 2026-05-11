using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Page.Ui.Domain.Chat.Entities;
using Page.Ui.Domain.Chat.Enums;
using Page.Ui.Infrastructure.Auth.Persistence;
using Page.Ui.Worker.Ai.Models;

namespace Page.Ui.Worker.Ai.Services;

public sealed class AiRunStorageService : IAiRunStorageService
{
    private const string BucketName = "ai-runs";
    private static readonly SemaphoreSlim BucketEnsureLock = new(1, 1);
    private static volatile bool _bucketConfirmed;

    private readonly ApplicationDbContext _dbContext;
    private readonly IMinioClient _minioClient;
    private readonly ILogger<AiRunStorageService> _logger;

    public AiRunStorageService(ApplicationDbContext dbContext, IMinioClient minioClient, ILogger<AiRunStorageService> logger)
    {
        _dbContext = dbContext;
        _minioClient = minioClient;
        _logger = logger;
    }

    public async Task<StoredAiRun> StoreAsync(AiChatContext context, AiModelResult result, Guid versionId, Guid runId, CancellationToken cancellationToken)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        var existingRun = await _dbContext.AiRuns
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
        if (existingRun is not null)
        {
            var existingFiles = await _dbContext.AiRunFiles
                .AsNoTracking()
                .Where(f => f.RunId == runId)
                .ToListAsync(cancellationToken);

            return new StoredAiRun
            {
                Run = existingRun,
                Files = existingFiles,
                UserStorageKey = BuildUserStorageKey(existingRun.OwnerUserId)
            };
        }

        var userStorageKey = BuildUserStorageKey(context.Chat.OwnerUserId);
        var chatKey = context.Chat.ChatKey;
        var objectPrefix = $"users/{userStorageKey}/chats/{chatKey}/versions/{versionId:D}";
        var now = DateTimeOffset.UtcNow;

        var title = string.IsNullOrWhiteSpace(result.Title)
            ? (string.IsNullOrWhiteSpace(context.Chat.Name) ? "Untitled Chat" : context.Chat.Name!)
            : result.Title!.Trim();

        var run = new AiRun
        {
            Id = runId,
            VersionId = versionId,
            ChatId = context.Chat.Id,
            OwnerUserId = context.Chat.OwnerUserId,
            TriggerMessageId = context.TriggerMessage.Id,
            ModelId = string.IsNullOrWhiteSpace(context.Chat.ModelId) ? "assistant-default" : context.Chat.ModelId,
            Title = title,
            IsCurrent = false,
            Status = AiRunStatus.Stored,
            ManifestObjectKey = $"{objectPrefix}/manifest.json",
            CreatedAt = now,
            UpdatedAt = now
        };

        var files = new List<AiRunFile>();
        var orderedFiles = result.Files
            .Where(f => !string.IsNullOrWhiteSpace(f.FileName))
            .Select((file, index) => new
            {
                File = file,
                SafeFileName = NormalizeStoredFileName(index, file.FileName)
            })
            .ToList();

        foreach (var file in orderedFiles)
        {
            var objectKey = file.File.ObjectKey ?? $"{objectPrefix}/source/{file.SafeFileName}";
            var contentType = string.IsNullOrWhiteSpace(file.File.ContentType) ? "text/plain" : file.File.ContentType;
            var runFile = new AiRunFile
            {
                RunId = run.Id,
                ObjectKey = objectKey,
                Role = InferRole(file.SafeFileName),
                OriginalFileName = file.File.FileName,
                StoredFileName = file.SafeFileName,
                ContentType = contentType,
                SizeBytes = 0,
                Sha256 = string.Empty,
                CreatedAt = now
            };

            if (file.File.Content is not null)
            {
                var contentBytes = Encoding.UTF8.GetBytes(file.File.Content);
                await using var contentStream = new MemoryStream(contentBytes, writable: false);
                await _minioClient.PutObjectAsync(
                    new PutObjectArgs()
                        .WithBucket(BucketName)
                        .WithObject(objectKey)
                        .WithStreamData(contentStream)
                        .WithObjectSize(contentBytes.LongLength)
                        .WithContentType(contentType),
                    cancellationToken);

                runFile.SizeBytes = contentBytes.LongLength;
                runFile.Sha256 = ComputeSha256(contentBytes);
            }

            files.Add(runFile);
        }

        _dbContext.AiRuns.Add(run);
        _dbContext.AiRunFiles.AddRange(files);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new StoredAiRun
        {
            Run = run,
            Files = files,
            UserStorageKey = userStorageKey
        };
    }

    public async Task<(string Html, string Css, string Js)> LoadRenderInputsAsync(StoredAiRun storedRun, CancellationToken cancellationToken)
    {
        var html = string.Empty;
        var css = string.Empty;
        var js = string.Empty;

        foreach (var file in storedRun.Files)
        {
            var content = await GetObjectContentAsync(file.ObjectKey, cancellationToken);
            switch (file.Role)
            {
                case "html":
                    html = content;
                    break;
                case "css":
                    css = content;
                    break;
                case "js":
                    js = content;
                    break;
            }
        }

        return (html, css, js);
    }

    public async Task PromoteCurrentAsync(StoredAiRun storedRun, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var previousCurrentRuns = await _dbContext.AiRuns
            .Where(r => r.ChatId == storedRun.Run.ChatId && r.IsCurrent && r.Id != storedRun.Run.Id)
            .ToListAsync(cancellationToken);

        foreach (var previous in previousCurrentRuns)
        {
            previous.IsCurrent = false;
            previous.UpdatedAt = now;
            previous.SupersededByRunId = storedRun.Run.Id;
        }

        storedRun.Run.IsCurrent = true;
        storedRun.Run.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(StoredAiRun storedRun, string failureCode, string failureMessage, CancellationToken cancellationToken)
    {
        storedRun.Run.Status = AiRunStatus.Failed;
        storedRun.Run.FailureCode = failureCode;
        storedRun.Run.FailureMessageSafe = failureMessage;
        storedRun.Run.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GetObjectContentAsync(string objectKey, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await _minioClient.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(BucketName)
                .WithObject(objectKey)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream)),
            cancellationToken);

        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        if (_bucketConfirmed)
        {
            return;
        }

        await BucketEnsureLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketConfirmed)
            {
                return;
            }

            var exists = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(BucketName), cancellationToken);
            if (!exists)
            {
                try
                {
                    await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(BucketName), cancellationToken);
                }
                catch (MinioException)
                {
                    exists = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(BucketName), cancellationToken);
                    if (!exists)
                    {
                        throw;
                    }
                }
            }

            _bucketConfirmed = true;
        }
        finally
        {
            BucketEnsureLock.Release();
        }
    }

    private static string BuildUserStorageKey(string userId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(userId.Trim().ToLowerInvariant()));
        return Convert.ToHexString(hash)[..24].ToLowerInvariant();
    }

    private static string NormalizeStoredFileName(int index, string? fileName)
    {
        var candidate = (fileName ?? $"file-{index + 1}")
            .Trim()
            .Replace('\\', '/');
        candidate = Path.GetFileName(candidate);
        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = $"file-{index + 1}.txt";
        }

        candidate = string.Concat(candidate.Select(ch => char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_' ? ch : '_'));
        return candidate.Length >= 4 && char.IsDigit(candidate[0]) && char.IsDigit(candidate[1]) && char.IsDigit(candidate[2])
            ? candidate
            : $"{index + 1:000}-{candidate}";
    }

    private static string InferRole(string storedFileName)
    {
        var extension = Path.GetExtension(storedFileName).ToLowerInvariant();
        return extension switch
        {
            ".html" => "html",
            ".css" => "css",
            ".js" => "js",
            _ => "asset"
        };
    }

    private static string ComputeSha256(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
