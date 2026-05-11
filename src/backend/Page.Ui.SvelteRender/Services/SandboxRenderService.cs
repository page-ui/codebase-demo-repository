using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Page.Ui.Domain.Chat.Enums;
using Page.Ui.SvelteRender.Models;
using Page.Ui.SvelteRender.Serialization;

namespace Page.Ui.SvelteRender.Services;

public class SandboxRenderService : INodeRenderService
{
    private readonly RenderOptions _renderOptions;
    private readonly SandboxOptions _sandboxOptions;
    private readonly ILogger<SandboxRenderService> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _semaphore;

    public SandboxRenderService(
        IOptions<RenderOptions> renderOptions,
        IOptions<SandboxOptions> sandboxOptions,
        ILogger<SandboxRenderService> logger,
        IWebHostEnvironment env,
        IServiceScopeFactory scopeFactory)
    {
        _renderOptions = renderOptions.Value;
        _sandboxOptions = sandboxOptions.Value;
        _logger = logger;
        _env = env;
        _scopeFactory = scopeFactory;
        _semaphore = new SemaphoreSlim(_sandboxOptions.MaxConcurrency, _sandboxOptions.MaxConcurrency);
    }

    public async Task<RenderResponse> CompileAsync(RenderRequest request)
    {
        var runId = request.RunId ?? Guid.NewGuid().ToString("N");
        var publicRunBasePath = RenderRunPath.GetPublicRunBasePath(request.Metadata, runId);

        await _semaphore.WaitAsync();
        try
        {
            var response = await SendRequestAsync(request, runId, publicRunBasePath);

            if (response != null && (response.Errors == null || response.Errors.Count == 0))
            {
                try
                {
                    CopyArtifacts(request, response, runId, publicRunBasePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to copy artifacts for run {RunId}", runId);
                    response.Errors ??= new List<string>();
                    response.Errors.Add("Failed to copy artifacts.");
                }
            }

            if (response is not null)
            {
                await RecordMetadataAsync(
                    request,
                    response,
                    RenderRunPath.GetRelativeRunPath(request.Metadata, runId),
                    response.Errors is { Count: > 0 } ? response.Errors[0] : null,
                    response.Errors is { Count: > 0 } ? RenderRunStatus.Failed : RenderRunStatus.Succeeded);
            }

            return response ?? new RenderResponse { RunId = runId, Errors = new List<string> { "No response from sandbox." } };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sandbox execution failed for run {RunId}", runId);
            return new RenderResponse
            {
                RunId = runId,
                Errors = new List<string> { $"Sandbox execution failed: {ex.Message}" }
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<RenderResponse?> SendRequestAsync(RenderRequest request, string runId, string publicRunBasePath)
    {
        var payload = new SandboxRenderPayload
        {
            Html = request.Html ?? string.Empty,
            Css = request.Css ?? string.Empty,
            Js = request.Js ?? string.Empty,
            Pages = request.Pages,
            RunId = runId,
            PublicRunBasePath = publicRunBasePath
        };

        var json = JsonSerializer.Serialize(payload, SvelteRenderJsonContext.Default.SandboxRenderPayload);
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        var uri = new Uri(_sandboxOptions.Endpoint);
        var host = uri.Host;
        var port = uri.Port;

        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_sandboxOptions.TimeoutSeconds));

        try
        {
            await socket.ConnectAsync(host, port, cts.Token);
        }
        catch (Exception ex)
        {
            return new RenderResponse { RunId = runId, Errors = new List<string> { $"Failed to connect to sandbox: {ex.Message}" } };
        }

        var lengthHeader = BitConverter.GetBytes(jsonBytes.Length);
        await socket.SendAsync(lengthHeader, SocketFlags.None, cts.Token);
        await socket.SendAsync(jsonBytes, SocketFlags.None, cts.Token);

        var buffer = new byte[4];
        var received = await ReceiveExactAsync(socket, buffer, 4, cts.Token);
        if (received != 4)
        {
            return new RenderResponse { RunId = runId, Errors = new List<string> { "Failed to read response length." } };
        }

        var responseLength = BitConverter.ToInt32(buffer, 0);
        if (responseLength <= 0 || responseLength > _sandboxOptions.MaxOutputBytes)
        {
            return new RenderResponse { RunId = runId, Errors = new List<string> { "Invalid response length." } };
        }

        var responseBuffer = new byte[responseLength];
        received = await ReceiveExactAsync(socket, responseBuffer, responseLength, cts.Token);
        if (received != responseLength)
        {
            return new RenderResponse { RunId = runId, Errors = new List<string> { "Failed to read response." } };
        }

        var responseJson = Encoding.UTF8.GetString(responseBuffer);
        var response = JsonSerializer.Deserialize(
            responseJson,
            SvelteRenderJsonContext.Default.RenderResponse);

        return response;
    }

    private static async Task<int> ReceiveExactAsync(Socket socket, byte[] buffer, int count, CancellationToken cancellationToken)
    {
        var totalReceived = 0;
        while (totalReceived < count)
        {
            var received = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, totalReceived, count - totalReceived), SocketFlags.None, cancellationToken);
            if (received == 0) break;
            totalReceived += received;
        }
        return totalReceived;
    }

    private void CopyArtifacts(RenderRequest request, RenderResponse response, string runId, string publicRunBasePath)
    {
        var runDir = RenderRunPath.GetPhysicalRunPath(_env.ContentRootPath, _renderOptions.RunsDirectory, request.Metadata, runId);
        if (Directory.Exists(runDir)) Directory.Delete(runDir, true);
        Directory.CreateDirectory(runDir);

        var inputDir = Path.Combine(runDir, "input");
        Directory.CreateDirectory(inputDir);
        File.WriteAllText(Path.Combine(inputDir, "input.html"), request.Html ?? string.Empty);
        File.WriteAllText(Path.Combine(inputDir, "input.css"), request.Css ?? string.Empty);
        File.WriteAllText(Path.Combine(inputDir, "input.js"), request.Js ?? string.Empty);
        if (request.Pages != null)
        {
            foreach (var page in request.Pages)
            {
                var pageInputDir = Path.Combine(inputDir, page.Path);
                Directory.CreateDirectory(pageInputDir);
                File.WriteAllText(Path.Combine(pageInputDir, "input.html"), page.Html ?? string.Empty);
                File.WriteAllText(Path.Combine(pageInputDir, "input.css"), page.Css ?? string.Empty);
                File.WriteAllText(Path.Combine(pageInputDir, "input.js"), page.Js ?? string.Empty);
            }
        }

        var artifactsDir = Path.Combine(runDir, "artifacts");
        Directory.CreateDirectory(artifactsDir);

        if (response.Artifacts != null)
        {
            foreach (var kvp in response.Artifacts)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    File.WriteAllText(RenderPathGuard.GetContainedPath(artifactsDir, kvp.Key), kvp.Value);
                }
            }
        }

        if (response.PreviewHtmls != null)
        {
            foreach (var kvp in response.PreviewHtmls)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    File.WriteAllText(RenderPathGuard.GetContainedPath(runDir, $"{kvp.Key}.html"), kvp.Value);
                }
            }
        }

        if (!string.IsNullOrEmpty(response.PreviewHtml))
        {
            File.WriteAllText(Path.Combine(runDir, "preview.html"), response.PreviewHtml);
        }

        response.ClientJsUrl = $"{publicRunBasePath}/artifacts/client.js";
        response.CssUrl = $"{publicRunBasePath}/artifacts/client.css";
        response.PreviewUrl = $"{publicRunBasePath}/preview.html";
        if (response.PreviewUrls is { Count: > 0 })
        {
            response.PreviewUrls = response.PreviewUrls.Keys
                .Order(StringComparer.Ordinal)
                .ToDictionary(path => path, path => $"{publicRunBasePath}/{path}.html", StringComparer.Ordinal);
        }

        File.WriteAllText(
            Path.Combine(runDir, "result.json"),
            JsonSerializer.Serialize(response, SvelteRenderJsonContext.Default.RenderResponse));
    }

    private async Task RecordMetadataAsync(RenderRequest request, RenderResponse response, string relativeRunPath, string? errorSummary, RenderRunStatus status)
    {
        using var scope = _scopeFactory.CreateScope();
        var metadataStore = scope.ServiceProvider.GetRequiredService<IRenderRunMetadataStore>();
        await metadataStore.RecordAsync(request, response, relativeRunPath, errorSummary, status, CancellationToken.None);
    }
}
