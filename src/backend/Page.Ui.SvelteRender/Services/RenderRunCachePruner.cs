using Microsoft.Extensions.Options;
using Page.Ui.SvelteRender.Models;

namespace Page.Ui.SvelteRender.Services;

public sealed class RenderRunCachePruner
{
    private readonly RenderOptions _options;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<RenderRunCachePruner> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _pruneLock = new(1, 1);
    private DateTimeOffset _lastPrunedAt = DateTimeOffset.MinValue;

    public RenderRunCachePruner(
        IOptions<RenderOptions> options,
        IWebHostEnvironment environment,
        ILogger<RenderRunCachePruner> logger,
        IServiceScopeFactory scopeFactory)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task PruneIfDueAsync(CancellationToken cancellationToken)
    {
        if (!_options.EnableRunCacheCleanup || _options.RunCacheMaxAgeHours <= 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var minInterval = TimeSpan.FromMinutes(Math.Max(1, _options.RunCacheCleanupIntervalMinutes));
        if (now - _lastPrunedAt < minInterval)
        {
            return;
        }

        if (!await _pruneLock.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            if (now - _lastPrunedAt < minInterval)
            {
                return;
            }

            var runsDirectory = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, _options.RunsDirectory));
            if (!Directory.Exists(runsDirectory))
            {
                _lastPrunedAt = now;
                return;
            }

            var cutoff = now - TimeSpan.FromHours(_options.RunCacheMaxAgeHours);
            foreach (var runDirectory in EnumerateRunDirectories(runsDirectory))
            {
                if (runDirectory.LastWriteTimeUtc >= cutoff.UtcDateTime)
                {
                    continue;
                }

                try
                {
                    var runId = ResolveRunId(runDirectory);
                    runDirectory.Delete(true);
                    await MarkPrunedAsync(runId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to prune cached render run {RunDirectory}", runDirectory.FullName);
                }
            }

            foreach (var parentDirectory in new DirectoryInfo(runsDirectory).EnumerateDirectories())
            {
                TryDeleteEmptyDirectories(parentDirectory);
            }

            _lastPrunedAt = now;
        }
        finally
        {
            _pruneLock.Release();
        }
    }

    private static IEnumerable<DirectoryInfo> EnumerateRunDirectories(string runsDirectory)
    {
        foreach (var directory in new DirectoryInfo(runsDirectory).EnumerateDirectories())
        {
            if (LooksLikeRunDirectory(directory))
            {
                yield return directory;
                continue;
            }

            foreach (var nestedDirectory in directory.EnumerateDirectories())
            {
                if (LooksLikeRunDirectory(nestedDirectory))
                {
                    yield return nestedDirectory;
                }
            }
        }
    }

    private static bool LooksLikeRunDirectory(DirectoryInfo directory)
    {
        return File.Exists(Path.Combine(directory.FullName, "result.json")) ||
               File.Exists(Path.Combine(directory.FullName, "preview.html"));
    }

    private static void TryDeleteEmptyDirectories(DirectoryInfo directory)
    {
        foreach (var child in directory.EnumerateDirectories())
        {
            TryDeleteEmptyDirectories(child);
        }

        if (!directory.EnumerateFileSystemInfos().Any())
        {
            directory.Delete();
        }
    }

    private async Task MarkPrunedAsync(string runId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var metadataStore = scope.ServiceProvider.GetRequiredService<IRenderRunMetadataStore>();
        await metadataStore.MarkPrunedAsync(runId, cancellationToken);
    }

    private static string ResolveRunId(DirectoryInfo runDirectory)
    {
        var resultPath = Path.Combine(runDirectory.FullName, "result.json");
        if (!File.Exists(resultPath))
        {
            return runDirectory.Name;
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(resultPath));
            if (document.RootElement.TryGetProperty("runId", out var runIdElement) &&
                runIdElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var runId = runIdElement.GetString();
                if (!string.IsNullOrWhiteSpace(runId))
                {
                    return runId;
                }
            }
        }
        catch
        {
        }

        return runDirectory.Name;
    }
}
