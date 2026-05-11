namespace Page.Ui.SvelteRender.Models;

public class RenderRequest
{
    public string Html { get; set; } = "";
    public string Css { get; set; } = "";
    public string Js { get; set; } = "";
    public List<RenderPage>? Pages { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();

    public string? RunId { get; set; }
}
