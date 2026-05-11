namespace Page.Ui.SvelteRender.Models;

public class RenderResponse
{
    public string RunId { get; set; } = string.Empty;
    public string SsrHtml { get; set; } = string.Empty;
    public string ClientJsUrl { get; set; } = string.Empty;
    public string CssUrl { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public Dictionary<string, string>? PreviewUrls { get; set; }
    public Dictionary<string, string>? SsrHtmls { get; set; }
    public List<string> Logs { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public Dictionary<string, string>? Artifacts { get; set; }
    public string? PreviewHtml { get; set; }
    public Dictionary<string, string>? PreviewHtmls { get; set; }
}
