namespace Page.Ui.SvelteRender.Models;

public class SandboxOptions
{
    public string Endpoint { get; set; } = "http://svelte-render-sandbox:4000";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxOutputBytes { get; set; } = 2 * 1024 * 1024;
    public bool DisableSsr { get; set; } = false;
    public int MaxConcurrency { get; set; } = 20;
}
