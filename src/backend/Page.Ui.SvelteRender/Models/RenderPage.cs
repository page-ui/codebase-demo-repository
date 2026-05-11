namespace Page.Ui.SvelteRender.Models;

public class RenderPage
{
    public string Path { get; set; } = "index";
    public string Html { get; set; } = string.Empty;
    public string Css { get; set; } = string.Empty;
    public string Js { get; set; } = string.Empty;
}
