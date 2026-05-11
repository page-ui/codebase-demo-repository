namespace Page.Ui.SvelteRender.Models;

public sealed class RenderDiagnosticsOptions
{
    public string? PageUiBaseUrl { get; set; }
    public string ReportPath { get; set; } = "api/internal/render-diagnostics/report";
    public string? RelayApiKey { get; set; }
}
