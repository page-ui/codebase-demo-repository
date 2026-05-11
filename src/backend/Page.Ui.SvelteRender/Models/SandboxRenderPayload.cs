namespace Page.Ui.SvelteRender.Models;

public sealed class SandboxRenderPayload
{
    public string Html { get; set; } = string.Empty;
    public string Css { get; set; } = string.Empty;
    public string Js { get; set; } = string.Empty;
    public List<RenderPage>? Pages { get; set; }
    public string RunId { get; set; } = string.Empty;
    public string PublicRunBasePath { get; set; } = string.Empty;
}
