namespace Page.Ui.SvelteRender.Models;

public sealed class RenderDiagnosticsReportRequest
{
    public string PublicRunToken { get; set; } = string.Empty;
    public string PagePath { get; set; } = "index";
    public List<RenderDiagnosticEntry> Entries { get; set; } = new();
}

public sealed class RenderDiagnosticEntry
{
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Stack { get; set; }
    public string? Source { get; set; }
    public int? Line { get; set; }
    public int? Column { get; set; }
    public string? Url { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}
