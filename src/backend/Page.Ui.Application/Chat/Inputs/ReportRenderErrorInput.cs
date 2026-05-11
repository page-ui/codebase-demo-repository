namespace Page.Ui.Application.Chat.Inputs;

public class ReportRenderErrorInput
{
    public string ChatKey { get; set; } = string.Empty;
    public Guid? VersionId { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Logs { get; set; } = new();
}
