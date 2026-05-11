namespace Page.Ui.Worker.Ai.Configuration;

public sealed class AiModelApiOptions
{
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string GeneratePath { get; set; } = "api/generate";
    public string ErrorReportPath { get; set; } = "api/report-error";
    public int TimeoutSeconds { get; set; } = 30;
}
