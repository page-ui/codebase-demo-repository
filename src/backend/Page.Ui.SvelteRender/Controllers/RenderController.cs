using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Page.Ui.SvelteRender.Models;
using Page.Ui.SvelteRender.Serialization;
using Page.Ui.SvelteRender.Services;
using Page.Ui.Domain.Chat.Enums;

namespace Page.Ui.SvelteRender.Controllers;

[ApiController]
[EnableRateLimiting("render")]
public class RenderController : ControllerBase
{
    private static readonly Regex StylesheetLinkRegex = new(
        "<link\\b(?=[^>]*\\brel\\s*=\\s*['\"]?stylesheet['\"]?)(?=[^>]*\\bhref\\s*=\\s*['\"](?<href>[^'\"]+)['\"])[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ScriptSrcRegex = new(
        "<script\\b(?=[^>]*\\bsrc\\s*=\\s*['\"](?<src>[^'\"]+)['\"])[^>]*>\\s*</script>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly INodeRenderService _renderService;
    private readonly RenderOptions _options;
    private readonly RenderRunCachePruner _cachePruner;
    private readonly IRenderRunMetadataStore _metadataStore;
    private readonly ILogger<RenderController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IMinioClient _minioClient;

    public RenderController(
        INodeRenderService renderService,
        IOptions<RenderOptions> options,
        RenderRunCachePruner cachePruner,
        IRenderRunMetadataStore metadataStore,
        ILogger<RenderController> logger,
        IWebHostEnvironment env,
        IMinioClient minioClient)
    {
        _renderService = renderService;
        _options = options.Value;
        _cachePruner = cachePruner;
        _metadataStore = metadataStore;
        _logger = logger;
        _env = env;
        _minioClient = minioClient;
    }

    [HttpPost("api/render-objects")]
    public async Task<IActionResult> RenderObjects([FromBody] RenderObjectRequest? request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest("Request body is required");
        }

        request.Metadata ??= new Dictionary<string, string>();
        request.Pages ??= new List<RenderObjectPage>();

        if (request.Metadata.Count > 100)
        {
            return BadRequest("Metadata too large");
        }

        if (request.Pages.Count == 0)
        {
            return BadRequest("At least one page is required");
        }

        if (request.Pages.Count > 100)
        {
            return BadRequest("Too many pages");
        }

        if (request.SourceFiles.Count > 200)
        {
            return BadRequest("Too many source files");
        }

        Dictionary<string, LoadedSourceFile> sourceBundle;
        try
        {
            sourceBundle = await LoadSourceBundleAsync(request.SourceFiles, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (MinioException ex)
        {
            _logger.LogWarning(ex, "Failed to read render source bundle");
            return BadRequest("Failed to read source bundle");
        }

        var pages = new List<RenderPage>();
        foreach (var page in request.Pages)
        {
            if (!TryNormalizePagePath(page.Path, out var normalizedPath, out var pathError))
            {
                return BadRequest(pathError);
            }

            if (string.IsNullOrWhiteSpace(page.HtmlObjectKey) &&
                string.IsNullOrWhiteSpace(page.CssObjectKey) &&
                string.IsNullOrWhiteSpace(page.JsObjectKey))
            {
                return BadRequest($"Page '{normalizedPath}' must include at least one source object key");
            }

            try
            {
                var renderPage = new RenderPage
                {
                    Path = normalizedPath,
                    Html = await ReadObjectOrEmptyAsync(page.HtmlObjectKey, _options.MaxHtmlSize, cancellationToken),
                    Css = await ReadObjectOrEmptyAsync(page.CssObjectKey, _options.MaxCssSize, cancellationToken),
                    Js = await ReadObjectOrEmptyAsync(page.JsObjectKey, _options.MaxJsSize, cancellationToken)
                };

                ResolveLinkedLocalSources(renderPage, sourceBundle);
                pages.Add(renderPage);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (MinioException ex)
            {
                _logger.LogWarning(ex, "Failed to read render source object for page {PagePath}", normalizedPath);
                return BadRequest($"Failed to read source object for page '{normalizedPath}'");
            }
        }

        RenderRequest renderRequest;
        try
        {
            renderRequest = new RenderRequest
            {
                Pages = NormalizePages(pages),
                Metadata = request.Metadata
            };
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        return await ProcessRenderRequest(renderRequest);
    }

    [HttpPost("api/render-form")]
    public async Task<IActionResult> RenderForm([FromForm] IFormFile? html, [FromForm] IFormFile? css, [FromForm] IFormFile? js, [FromForm] string? metadata)
    {
        if (html?.Length > _options.MaxHtmlSize) return BadRequest("HTML too large");
        if (css?.Length > _options.MaxCssSize) return BadRequest("CSS too large");
        if (js?.Length > _options.MaxJsSize) return BadRequest("JS too large");

        var request = new RenderRequest();

        if (html != null) { using var reader = new StreamReader(html.OpenReadStream()); request.Html = await reader.ReadToEndAsync(); }
        if (css != null) { using var reader = new StreamReader(css.OpenReadStream()); request.Css = await reader.ReadToEndAsync(); }
        if (js != null) { using var reader = new StreamReader(js.OpenReadStream()); request.Js = await reader.ReadToEndAsync(); }

        if (!string.IsNullOrWhiteSpace(metadata))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize(
                    metadata,
                    SvelteRenderJsonContext.Default.DictionaryStringString);
                if (parsed == null)
                {
                    return BadRequest("Metadata must be a JSON object");
                }
                request.Metadata = parsed;
            }
            catch (JsonException)
            {
                return BadRequest("Metadata must be valid JSON");
            }
        }

        if (string.IsNullOrWhiteSpace(request.Html) &&
            string.IsNullOrWhiteSpace(request.Css) &&
            string.IsNullOrWhiteSpace(request.Js))
        {
            return BadRequest("At least one input is required");
        }

        request.Pages = new List<RenderPage>
        {
            new()
            {
                Path = "index",
                Html = request.Html,
                Css = request.Css,
                Js = request.Js
            }
        };

        return await ProcessRenderRequest(request);
    }

    [HttpGet("api/runs/{runId}")]
    public async Task<IActionResult> GetRun(string runId, CancellationToken cancellationToken)
    {
        var run = await _metadataStore.GetByRunIdAsync(runId, cancellationToken);
        return run is null ? NotFound() : Ok(run);
    }

    [HttpGet("api/runs/public/{publicRunToken}")]
    public async Task<IActionResult> GetByPublicToken(string publicRunToken, CancellationToken cancellationToken)
    {
        var run = await _metadataStore.GetByPublicRunTokenAsync(publicRunToken, cancellationToken);
        return run is null ? NotFound() : Ok(run);
    }

    [HttpGet("api/runs/by-message/{messageId:guid}")]
    public async Task<IActionResult> GetByMessage(Guid messageId, CancellationToken cancellationToken)
    {
        var run = await _metadataStore.GetByMessageIdAsync(messageId, cancellationToken);
        return run is null ? NotFound() : Ok(run);
    }

    private async Task<IActionResult> ProcessRenderRequest(RenderRequest request)
    {
        await _cachePruner.PruneIfDueAsync(HttpContext.RequestAborted);
        request.Metadata ??= new Dictionary<string, string>();

        if (request.Metadata.Count > 100)
        {
            return BadRequest("Metadata too large");
        }

        List<RenderPage> pages;
        try
        {
            pages = NormalizePages(request.Pages ?? new List<RenderPage>());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        if (pages.Count == 0)
        {
            return BadRequest("At least one page is required");
        }

        long totalHtmlSize = 0;
        long totalCssSize = 0;
        long totalJsSize = 0;
        foreach (var page in pages)
        {
            totalHtmlSize += Encoding.UTF8.GetByteCount(page.Html);
            totalCssSize += Encoding.UTF8.GetByteCount(page.Css);
            totalJsSize += Encoding.UTF8.GetByteCount(page.Js);
        }

        if (totalHtmlSize > _options.MaxHtmlSize) return BadRequest("HTML too large");
        if (totalCssSize > _options.MaxCssSize) return BadRequest("CSS too large");
        if (totalJsSize > _options.MaxJsSize) return BadRequest("JS too large");

        request.Pages = pages;
        var indexPage = pages.FirstOrDefault(page => page.Path == "index") ?? pages[0];
        request.Html = indexPage.Html;
        request.Css = indexPage.Css;
        request.Js = indexPage.Js;

        var normalizedMetadata = new SortedDictionary<string, string>(request.Metadata, StringComparer.Ordinal);
        var pagesJson = JsonSerializer.Serialize(pages, SvelteRenderJsonContext.Default.ListRenderPage);
        var inputString = pagesJson + JsonSerializer.Serialize(
            normalizedMetadata,
            SvelteRenderJsonContext.Default.SortedDictionaryStringString);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        var hexHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        var runId = $"{hexHash.Substring(0, 8)}-{hexHash.Substring(8, 4)}-{hexHash.Substring(12, 4)}-{hexHash.Substring(16, 4)}-{hexHash.Substring(20, 12)}";
        request.RunId = runId;
        request.Metadata["publicRunToken"] = RenderRunPublicToken.FromRunContext(runId, request.Metadata);

        var runDir = RenderRunPath.GetPhysicalRunPath(_env.ContentRootPath, _options.RunsDirectory, request.Metadata, runId);
        var cachedResultPath = Path.Combine(runDir, "result.json");
        var forceRefresh = Request.Query.ContainsKey("refresh");

        if (System.IO.File.Exists(cachedResultPath) && !forceRefresh)
        {
            try
            {
                var cachedResponse = await System.IO.File.ReadAllTextAsync(cachedResultPath);
                var response = JsonSerializer.Deserialize(
                    cachedResponse,
                    SvelteRenderJsonContext.Default.RenderResponse);
                if (response != null)
                {
                    _logger.LogInformation("Cache hit for run {RunId}", runId);
                    await _metadataStore.RecordAsync(request, response, RenderRunPath.GetRelativeRunPath(request.Metadata, runId), null, RenderRunStatus.Succeeded, HttpContext.RequestAborted);
                    ApplyPublicRunUrls(response, request.Metadata);
                    return Ok(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read cache for run {RunId}", runId);
            }
        }

        var result = await _renderService.CompileAsync(request);

        result.Artifacts = null;
        result.PreviewHtml = null;
        result.PreviewHtmls = null;
        result.Logs = new List<string>();
        ApplyPublicRunUrls(result, request.Metadata);

        return Ok(result);
    }

    private static void ApplyPublicRunUrls(RenderResponse response, IReadOnlyDictionary<string, string> metadata)
    {
        if (string.IsNullOrWhiteSpace(response.RunId))
        {
            return;
        }

        var publicBasePath = RenderRunPublicToken.BuildPublicBasePath(response.RunId, metadata);
        response.ClientJsUrl = $"{publicBasePath}/artifacts/client.js";
        response.CssUrl = $"{publicBasePath}/artifacts/client.css";
        response.PreviewUrl = $"{publicBasePath}/preview.html";

        if (response.PreviewUrls is { Count: > 0 })
        {
            response.PreviewUrls = response.PreviewUrls.Keys
                .Order(StringComparer.Ordinal)
                .ToDictionary(path => path, path => $"{publicBasePath}/{path}.html", StringComparer.Ordinal);
        }
    }

    private async Task<string> ReadObjectOrEmptyAsync(string? objectKey, int maxBytes, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            return string.Empty;
        }

        using var memoryStream = new MemoryStream();
        await _minioClient.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(_options.SourceBucketName)
                .WithObject(objectKey.Trim())
                .WithCallbackStream(stream => CopyWithLimit(stream, memoryStream, maxBytes, objectKey)),
            cancellationToken);

        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    private async Task<Dictionary<string, LoadedSourceFile>> LoadSourceBundleAsync(
        IReadOnlyList<RenderSourceFile> sourceFiles,
        CancellationToken cancellationToken)
    {
        var bundle = new Dictionary<string, LoadedSourceFile>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in sourceFiles)
        {
            if (string.IsNullOrWhiteSpace(file.FileName) || string.IsNullOrWhiteSpace(file.ObjectKey))
            {
                continue;
            }

            if (!TryNormalizeBundlePath(file.FileName, out var normalizedPath))
            {
                throw new InvalidOperationException($"Source file path '{file.FileName}' is not allowed");
            }

            var maxBytes = GetSourceMaxBytes(file.FileName, file.ContentType);
            var content = await ReadObjectOrEmptyAsync(file.ObjectKey, maxBytes, cancellationToken);
            bundle[normalizedPath] = new LoadedSourceFile(normalizedPath, file.ContentType ?? string.Empty, content);
        }

        return bundle;
    }

    private void ResolveLinkedLocalSources(RenderPage page, IReadOnlyDictionary<string, LoadedSourceFile> sourceBundle)
    {
        page.Css = ResolveLocalStylesheets(page.Html, page.Css, sourceBundle);
        page.Js = ResolveLocalScripts(page.Html, page.Js, sourceBundle);
    }

    private string ResolveLocalStylesheets(
        string html,
        string existingCss,
        IReadOnlyDictionary<string, LoadedSourceFile> sourceBundle)
    {
        var resolvedCss = new StringBuilder(existingCss ?? string.Empty);

        foreach (Match match in StylesheetLinkRegex.Matches(html ?? string.Empty))
        {
            var href = match.Groups["href"].Value;
            if (!TryNormalizeLocalReference(href, out var normalizedReference, out var isExternal))
            {
                if (!isExternal)
                {
                    throw new InvalidOperationException($"Stylesheet reference '{href}' is not allowed");
                }

                continue;
            }

            if (sourceBundle.TryGetValue(normalizedReference, out var source))
            {
                AppendResolvedSourceIfMissing(
                    resolvedCss,
                    existingCss,
                    source.Content,
                    $"/* Resolved local stylesheet: {normalizedReference} */");
            }
            else if (string.IsNullOrWhiteSpace(existingCss))
            {
                throw new InvalidOperationException($"Stylesheet reference '{href}' was not included in the submitted source bundle");
            }
        }

        return resolvedCss.ToString();
    }

    private string ResolveLocalScripts(
        string html,
        string existingJs,
        IReadOnlyDictionary<string, LoadedSourceFile> sourceBundle)
    {
        var resolvedJs = new StringBuilder(existingJs ?? string.Empty);

        foreach (Match match in ScriptSrcRegex.Matches(html ?? string.Empty))
        {
            var src = match.Groups["src"].Value;
            if (!TryNormalizeLocalReference(src, out var normalizedReference, out var isExternal))
            {
                if (!isExternal)
                {
                    throw new InvalidOperationException($"Script reference '{src}' is not allowed");
                }

                continue;
            }

            if (sourceBundle.TryGetValue(normalizedReference, out var source))
            {
                AppendResolvedSourceIfMissing(
                    resolvedJs,
                    existingJs,
                    source.Content,
                    $"/* Resolved local script: {normalizedReference} */");
            }
            else if (string.IsNullOrWhiteSpace(existingJs))
            {
                throw new InvalidOperationException($"Script reference '{src}' was not included in the submitted source bundle");
            }
        }

        return resolvedJs.ToString();
    }

    private static void AppendResolvedSourceIfMissing(
        StringBuilder builder,
        string? existingContent,
        string sourceContent,
        string marker)
    {
        if (string.IsNullOrWhiteSpace(sourceContent))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(existingContent) &&
            existingContent.Contains(sourceContent, StringComparison.Ordinal))
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine(marker);
        builder.AppendLine(sourceContent);
    }

    private int GetSourceMaxBytes(string fileName, string? contentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var type = (contentType ?? string.Empty).ToLowerInvariant();

        if (extension == ".css" || type.Contains("css", StringComparison.Ordinal))
        {
            return _options.MaxCssSize;
        }

        if (extension == ".js" || extension == ".mjs" || type.Contains("javascript", StringComparison.Ordinal))
        {
            return _options.MaxJsSize;
        }

        return _options.MaxHtmlSize;
    }

    private static bool TryNormalizeLocalReference(string rawReference, out string normalizedPath, out bool isExternal)
    {
        normalizedPath = string.Empty;
        isExternal = false;
        var reference = (rawReference ?? string.Empty).Trim();
        if (reference.Length == 0)
        {
            return false;
        }

        if (reference.StartsWith("//", StringComparison.Ordinal) ||
            Uri.TryCreate(reference, UriKind.Absolute, out _))
        {
            isExternal = true;
            return false;
        }

        var queryIndex = reference.IndexOfAny(['?', '#']);
        if (queryIndex >= 0)
        {
            reference = reference[..queryIndex];
        }

        reference = reference.Replace('\\', '/');
        if (reference.StartsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        return TryNormalizeBundlePath(reference, out normalizedPath);
    }

    private static bool TryNormalizeBundlePath(string rawPath, out string normalizedPath)
    {
        normalizedPath = string.Empty;
        var path = (rawPath ?? string.Empty).Trim().Replace('\\', '/');
        if (path.Length == 0 || path.StartsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        var queryIndex = path.IndexOfAny(['?', '#']);
        if (queryIndex >= 0)
        {
            path = path[..queryIndex];
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(segment => segment != ".")
            .ToArray();
        if (segments.Length == 0 || segments.Any(segment => segment == ".."))
        {
            return false;
        }

        normalizedPath = string.Join('/', segments).ToLowerInvariant();
        return true;
    }

    private static void CopyWithLimit(Stream source, Stream destination, int maxBytes, string objectKey)
    {
        var buffer = new byte[81920];
        var total = 0;
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            total += read;
            if (total > maxBytes)
            {
                throw new InvalidOperationException($"Source object '{objectKey}' is too large");
            }

            destination.Write(buffer, 0, read);
        }
    }

    private static List<RenderPage> NormalizePages(List<RenderPage> pages)
    {
        var normalized = new List<RenderPage>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var page in pages)
        {
            if (!TryNormalizePagePath(page.Path, out var path, out var error))
            {
                throw new InvalidOperationException(error);
            }

            if (!seen.Add(path))
            {
                throw new InvalidOperationException($"Duplicate page path '{path}'");
            }

            normalized.Add(new RenderPage
            {
                Path = path,
                Html = page.Html ?? string.Empty,
                Css = page.Css ?? string.Empty,
                Js = page.Js ?? string.Empty
            });
        }

        return normalized.OrderBy(page => page.Path, StringComparer.Ordinal).ToList();
    }

    private static bool TryNormalizePagePath(string? rawPath, out string path, out string error)
    {
        path = (rawPath ?? string.Empty).Trim();
        error = string.Empty;

        if (path.Length == 0)
        {
            error = "Page path is required";
            return false;
        }

        if (path is "." or ".." or "preview")
        {
            error = $"Page path '{path}' is reserved";
            return false;
        }

        if (path.Contains('/') || path.Contains('\\') || path.Contains('.') || path != path.ToLowerInvariant())
        {
            error = $"Page path '{path}' must be a lowercase slug";
            return false;
        }

        foreach (var ch in path)
        {
            if (!char.IsAsciiLetterOrDigit(ch) && ch != '-')
            {
                error = $"Page path '{path}' must be a lowercase slug";
                return false;
            }
        }

        return true;
    }

    private sealed record LoadedSourceFile(string Path, string ContentType, string Content);
}
