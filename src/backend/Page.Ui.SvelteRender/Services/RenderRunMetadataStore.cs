using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Page.Ui.Domain.Chat.Entities;
using Page.Ui.Domain.Chat.Enums;
using Page.Ui.Infrastructure.Auth.Persistence;
using Page.Ui.SvelteRender.Models;
using Page.Ui.SvelteRender.Serialization;
using StackExchange.Redis;

namespace Page.Ui.SvelteRender.Services;

public sealed class RenderRunMetadataStore : IRenderRunMetadataStore
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RenderRunMetadataStore> _logger;

    public RenderRunMetadataStore(
        ApplicationDbContext dbContext,
        IConnectionMultiplexer redis,
        ILogger<RenderRunMetadataStore> logger)
    {
        _dbContext = dbContext;
        _redis = redis;
        _logger = logger;
    }

    public async Task RecordAsync(RenderRequest request, RenderResponse response, string relativeRunPath, string? errorSummary, RenderRunStatus status, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var metadataJson = JsonSerializer.Serialize(request.Metadata);
        var runId = response.RunId;
        var userId = TryGetValue(request.Metadata, "userId");
        var userStorageKey = TryGetValue(request.Metadata, "userStorageKey");
        var chatKey = TryGetValue(request.Metadata, "chatKey");
        var chatId = TryParseGuid(request.Metadata, "chatId");
        var messageId = TryParseGuid(request.Metadata, "messageId");
        var versionId = TryParseGuid(request.Metadata, "versionId");
        var publicRunToken = RenderRunPublicToken.FromRunContext(runId, request.Metadata);

        var entity = await _dbContext.RenderRuns.FirstOrDefaultAsync(r => r.RunId == runId, cancellationToken);
        if (entity is null)
        {
            entity = new RenderRun
            {
                RunId = runId,
                PublicRunToken = publicRunToken,
                CreatedAtUtc = now
            };

            _dbContext.RenderRuns.Add(entity);
        }
        else
        {
            entity.PublicRunToken = publicRunToken;
        }

        entity.UserId = userId;
        entity.UserStorageKey = userStorageKey;
        entity.ChatId = chatId;
        entity.ChatKey = chatKey;
        entity.MessageId = messageId;
        entity.VersionId = versionId;
        entity.LastAccessedAtUtc = now;
        entity.Status = status;
        entity.RelativeRunPath = relativeRunPath;
        entity.PreviewUrl = $"/runs/{entity.PublicRunToken}/preview.html";
        entity.ErrorSummary = errorSummary;
        entity.MetadataJson = metadataJson;
        var sourceJson = request.Pages is { Count: > 0 }
            ? JsonSerializer.Serialize(request.Pages, SvelteRenderJsonContext.Default.ListRenderPage)
            : JsonSerializer.Serialize(
                new List<RenderPage>
                {
                    new()
                    {
                        Path = "index",
                        Html = request.Html,
                        Css = request.Css,
                        Js = request.Js
                    }
                },
                SvelteRenderJsonContext.Default.ListRenderPage);
        entity.ContentHash = ComputeSha256(sourceJson, metadataJson);
        entity.SourceHash = ComputeSha256(sourceJson);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await CacheAsync(entity, cancellationToken);
    }

    public async Task<RenderRun?> GetByRunIdAsync(string runId, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var cachedJson = await db.StringGetAsync($"render:run:{runId}");
        if (cachedJson.HasValue)
        {
            return JsonSerializer.Deserialize<RenderRun>(cachedJson.ToString());
        }

        var entity = await _dbContext.RenderRuns.AsNoTracking().FirstOrDefaultAsync(r => r.RunId == runId, cancellationToken);
        if (entity is not null)
        {
            await CacheAsync(entity, cancellationToken);
        }

        return entity;
    }

    public async Task<RenderRun?> GetByPublicRunTokenAsync(string publicRunToken, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var cachedRunId = await db.StringGetAsync($"render:public:{publicRunToken}");
        if (cachedRunId.HasValue)
        {
            return await GetByRunIdAsync(cachedRunId.ToString(), cancellationToken);
        }

        var entity = await _dbContext.RenderRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.PublicRunToken == publicRunToken, cancellationToken);

        if (entity is null)
        {
            entity = await GetByLegacyPublicRunTokenAsync(publicRunToken, cancellationToken);
        }

        if (entity is not null)
        {
            await CacheAsync(entity, cancellationToken);
        }

        return entity;
    }

    private async Task<RenderRun?> GetByLegacyPublicRunTokenAsync(string publicRunToken, CancellationToken cancellationToken)
    {
        var candidates = await _dbContext.RenderRuns
            .AsNoTracking()
            .Where(r => r.Status != RenderRunStatus.Pruned)
            .Select(r => new { r.RunId })
            .ToListAsync(cancellationToken);
        var match = candidates.FirstOrDefault(candidate =>
            string.Equals(RenderRunPublicToken.FromRunId(candidate.RunId), publicRunToken, StringComparison.Ordinal));
        if (match is null)
        {
            return null;
        }

        var entity = await _dbContext.RenderRuns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RunId == match.RunId, cancellationToken);
        if (entity is not null)
        {
            await _redis.GetDatabase().StringSetAsync($"render:public:{publicRunToken}", entity.RunId, TimeSpan.FromHours(12));
        }

        return entity;
    }

    public async Task<RenderRun?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var cachedRunId = await db.StringGetAsync($"render:message:{messageId:D}");
        if (cachedRunId.HasValue)
        {
            return await GetByRunIdAsync(cachedRunId!, cancellationToken);
        }

        var entity = await _dbContext.RenderRuns
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAtUtc)
            .FirstOrDefaultAsync(r => r.MessageId == messageId, cancellationToken);

        if (entity is not null)
        {
            await CacheAsync(entity, cancellationToken);
        }

        return entity;
    }

    public Task<IReadOnlyList<RenderRun>> GetByUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken)
        => GetListAsync(userId, null, page, pageSize, cancellationToken);

    public Task<IReadOnlyList<RenderRun>> GetByChatIdAsync(Guid chatId, int page, int pageSize, CancellationToken cancellationToken)
        => GetListAsync(null, chatId, page, pageSize, cancellationToken);

    public async Task MarkPrunedAsync(string runId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.RenderRuns.FirstOrDefaultAsync(r => r.RunId == runId, cancellationToken);
        if (entity is null)
        {
            return;
        }

        entity.Status = RenderRunStatus.Pruned;
        entity.LastAccessedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync($"render:run:{runId}");
        await db.KeyDeleteAsync($"render:public:{entity.PublicRunToken}");
        if (entity.MessageId.HasValue)
        {
            await db.KeyDeleteAsync($"render:message:{entity.MessageId.Value:D}");
        }

        if (!string.IsNullOrWhiteSpace(entity.UserId))
        {
            await db.SortedSetRemoveAsync($"render:user:{entity.UserId}:runs", runId);
        }

        if (entity.ChatId.HasValue)
        {
            await db.SortedSetRemoveAsync($"render:chat:{entity.ChatId.Value:D}:runs", runId);
        }
    }

    private async Task<IReadOnlyList<RenderRun>> GetListAsync(string? userId, Guid? chatId, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var db = _redis.GetDatabase();

        string? sortedSetKey = userId is not null
            ? $"render:user:{userId}:runs"
            : chatId.HasValue ? $"render:chat:{chatId.Value:D}:runs" : null;

        if (!string.IsNullOrWhiteSpace(sortedSetKey))
        {
            var cachedRunIds = await db.SortedSetRangeByRankAsync(
                sortedSetKey,
                (page - 1) * pageSize,
                (page * pageSize) - 1,
                Order.Descending);

            if (cachedRunIds.Length > 0)
            {
                var runs = await GetCachedRunsAsync(cachedRunIds, cancellationToken);
                if (runs.Count > 0)
                {
                    return runs;
                }
            }
        }

        IQueryable<RenderRun> query = _dbContext.RenderRuns.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(r => r.UserId == userId);
        }

        if (chatId.HasValue)
        {
            query = query.Where(r => r.ChatId == chatId);
        }

        var entities = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        foreach (var entity in entities)
        {
            await CacheAsync(entity, cancellationToken);
        }

        return entities;
    }

    private async Task<IReadOnlyList<RenderRun>> GetCachedRunsAsync(RedisValue[] runIds, CancellationToken cancellationToken)
    {
        var ids = runIds
            .Where(id => id.HasValue)
            .Select(id => id.ToString())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToList();
        if (ids.Count == 0)
        {
            return Array.Empty<RenderRun>();
        }

        var db = _redis.GetDatabase();
        var cacheKeys = ids.Select(id => (RedisKey)$"render:run:{id}").ToArray();
        var cachedValues = await db.StringGetAsync(cacheKeys);
        var runsById = new Dictionary<string, RenderRun>(StringComparer.Ordinal);
        var missingIds = new List<string>();

        for (var i = 0; i < ids.Count; i++)
        {
            var cachedValue = cachedValues[i];
            if (!cachedValue.HasValue)
            {
                missingIds.Add(ids[i]);
                continue;
            }

            try
            {
                var run = JsonSerializer.Deserialize<RenderRun>(cachedValue.ToString());
                if (run is not null)
                {
                    runsById[ids[i]] = run;
                    continue;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize cached render run metadata for {RunId}", ids[i]);
            }

            missingIds.Add(ids[i]);
        }

        if (missingIds.Count > 0)
        {
            var missingRuns = await _dbContext.RenderRuns
                .AsNoTracking()
                .Where(r => missingIds.Contains(r.RunId))
                .ToListAsync(cancellationToken);

            foreach (var run in missingRuns)
            {
                runsById[run.RunId] = run;
                await CacheAsync(run, cancellationToken);
            }
        }

        return ids
            .Select(id => runsById.TryGetValue(id, out var run) ? run : null)
            .Where(run => run is not null)
            .Cast<RenderRun>()
            .ToList();
    }

    private async Task CacheAsync(RenderRun entity, CancellationToken cancellationToken)
    {
        try
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(entity);
            await db.StringSetAsync($"render:run:{entity.RunId}", json, TimeSpan.FromHours(12));
            await db.StringSetAsync($"render:public:{entity.PublicRunToken}", entity.RunId, TimeSpan.FromHours(12));

            if (entity.MessageId.HasValue)
            {
                await db.StringSetAsync($"render:message:{entity.MessageId.Value:D}", entity.RunId, TimeSpan.FromHours(12));
            }

            if (!string.IsNullOrWhiteSpace(entity.UserId))
            {
                await db.SortedSetAddAsync($"render:user:{entity.UserId}:runs", entity.RunId, entity.CreatedAtUtc.ToUnixTimeSeconds());
            }

            if (entity.ChatId.HasValue)
            {
                await db.SortedSetAddAsync($"render:chat:{entity.ChatId.Value:D}:runs", entity.RunId, entity.CreatedAtUtc.ToUnixTimeSeconds());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache render run metadata for {RunId}", entity.RunId);
        }
    }

    private static string? TryGetValue(IReadOnlyDictionary<string, string> metadata, string key)
    {
        return metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : null;
    }

    private static Guid? TryParseGuid(IReadOnlyDictionary<string, string> metadata, string key)
    {
        var raw = TryGetValue(metadata, key);
        return Guid.TryParse(raw, out var value) ? value : null;
    }

    private static string ComputeSha256(params string[] parts)
    {
        using var sha = SHA256.Create();
        var combined = string.Join("\n--PART--\n", parts);
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(combined))).ToLowerInvariant();
    }
}
