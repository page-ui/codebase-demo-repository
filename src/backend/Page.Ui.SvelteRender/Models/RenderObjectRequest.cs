namespace Page.Ui.SvelteRender.Models;

public class RenderObjectRequest
{
    public List<RenderObjectPage> Pages { get; set; } = new();
    public List<RenderSourceFile> SourceFiles { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class RenderObjectPage
{
    public string Path { get; set; } = "index";
    public string? HtmlObjectKey { get; set; }
    public string? CssObjectKey { get; set; }
    public string? JsObjectKey { get; set; }
}

public class RenderSourceFile
{
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string ObjectKey { get; set; } = string.Empty;
}
