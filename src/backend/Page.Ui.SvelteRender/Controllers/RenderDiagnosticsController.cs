using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Page.Ui.SvelteRender.Models;
using Page.Ui.SvelteRender.Services;

namespace Page.Ui.SvelteRender.Controllers;

[ApiController]
[Route("api/render-diagnostics")]
[EnableRateLimiting("render")]
public sealed class RenderDiagnosticsController : ControllerBase
{
    private const int MaxEntries = 50;
    private const int MaxEntryLength = 1_000;

    private readonly IRenderRunMetadataStore _metadataStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RenderDiagnosticsOptions _options;
    private readonly ILogger<RenderDiagnosticsController> _logger;

    public RenderDiagnosticsController(
        IRenderRunMetadataStore metadataStore,
        IHttpClientFactory httpClientFactory,
        IOptions<RenderDiagnosticsOptions> options,
        ILogger<RenderDiagnosticsController> logger)
    {
        _metadataStore = metadataStore;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    [HttpPost("report")]
    public async Task<IActionResult> Report(
        [FromBody] RenderDiagnosticsReportRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.PublicRunToken))
        {
            return BadRequest(new { error = "publicRunToken is required." });
        }

        var run = await _metadataStore.GetByPublicRunTokenAsync(request.PublicRunToken.Trim(), cancellationToken);
        if (run is null || string.IsNullOrWhiteSpace(run.ChatKey) || !run.VersionId.HasValue)
        {
            return NotFound(new { error = "Render run was not found." });
        }

        var errors = new List<string>();
        var logs = new List<string>();
        foreach (var entry in request.Entries.Take(MaxEntries))
        {
            var line = FormatEntry(request.PagePath, entry);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (IsError(entry.Severity))
            {
                errors.Add(line);
            }
            else
            {
                logs.Add(line);
            }
        }

        if (errors.Count == 0 && logs.Count == 0)
        {
            return BadRequest(new { error = "At least one diagnostic entry is required." });
        }

        if (string.IsNullOrWhiteSpace(_options.PageUiBaseUrl) || string.IsNullOrWhiteSpace(_options.RelayApiKey))
        {
            _logger.LogWarning("Render diagnostics relay is not configured.");
            return Accepted(new { relayed = false });
        }

        try
        {
            var client = _httpClientFactory.CreateClient("PageUiDiagnostics");
            using var message = new HttpRequestMessage(HttpMethod.Post, _options.ReportPath)
            {
                Content = JsonContent.Create(new
                {
                    chatKey = run.ChatKey,
                    versionId = run.VersionId,
                    publicRunToken = run.PublicRunToken,
                    pagePath = request.PagePath,
                    errors,
                    logs
                })
            };
            message.Headers.Add("X-Render-Diagnostics-Key", _options.RelayApiKey);

            using var response = await client.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Page.Ui render diagnostics relay returned {StatusCode} for run {RunId}.",
                    (int)response.StatusCode,
                    run.RunId);
            }

            return Accepted(new { relayed = response.IsSuccessStatusCode });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to relay render diagnostics for run {RunId}.", run.RunId);
            return Accepted(new { relayed = false });
        }
    }

    private static bool IsError(string? severity)
    {
        return string.Equals(severity, "error", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(severity, "unhandledrejection", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatEntry(string pagePath, RenderDiagnosticEntry entry)
    {
        var severity = string.IsNullOrWhiteSpace(entry.Severity) ? "log" : entry.Severity.Trim();
        var message = Sanitize(entry.Message);
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        var location = string.IsNullOrWhiteSpace(entry.Source)
            ? string.Empty
            : $" source={Sanitize(entry.Source)}";
        var line = entry.Line.HasValue ? $" line={entry.Line.Value}" : string.Empty;
        var column = entry.Column.HasValue ? $" column={entry.Column.Value}" : string.Empty;
        var stack = string.IsNullOrWhiteSpace(entry.Stack) ? string.Empty : $" stack={Sanitize(entry.Stack)}";
        var url = string.IsNullOrWhiteSpace(entry.Url) ? string.Empty : $" url={Sanitize(entry.Url)}";

        return Sanitize($"[{severity}] page={pagePath}{url}{location}{line}{column} message={message}{stack}");
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Trim();
        normalized = new string(normalized
            .Select(ch => char.IsControl(ch) && ch != '\n' ? ' ' : ch)
            .ToArray());

        return normalized.Length <= MaxEntryLength ? normalized : normalized[..MaxEntryLength];
    }
}
