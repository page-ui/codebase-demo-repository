namespace Page.Ui.SvelteRender.Models;

public class RenderOptions
{
    public int WorkerPort { get; set; } = 3000;
    public string RunsDirectory { get; set; } = "NodeWorker/runs";
    public int MaxHtmlSize { get; set; } = 200 * 1024;
    public int MaxCssSize { get; set; } = 300 * 1024;
    public int MaxJsSize { get; set; } = 300 * 1024;
    public string SourceBucketName { get; set; } = "ai-runs";
    public int WorkerCount { get; set; } = 4;
    public bool EnableRunCacheCleanup { get; set; }
    public int RunCacheMaxAgeHours { get; set; }
    public int RunCacheCleanupIntervalMinutes { get; set; } = 30;
}
