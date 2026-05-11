using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Minio;
using Page.Ui.SvelteRender.Health;
using Page.Ui.SvelteRender.Models;
using Page.Ui.SvelteRender.Serialization;
using Page.Ui.SvelteRender.Services;
using Page.Ui.Infrastructure.Auth.Persistence;
using Page.Ui.Infrastructure.Configuration;
using Serilog;
using Npgsql;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "Page.Ui.SvelteRender");
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.MaxDepth = 64;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, SvelteRenderJsonContext.Default);
    });
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.Configure<RenderOptions>(builder.Configuration.GetSection("RenderOptions"));
builder.Services.Configure<RenderDiagnosticsOptions>(builder.Configuration.GetSection("RenderDiagnostics"));
builder.Services.Configure<RenderRateLimitOptions>(builder.Configuration.GetSection("RenderRateLimit"));
builder.Services.Configure<SandboxOptions>(builder.Configuration.GetSection("Sandbox"));
var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databasePoolingOptions = builder.Configuration.GetSection("Database:Pooling").Get<DatabasePoolingOptions>() ?? new();
if (!string.IsNullOrWhiteSpace(defaultConnectionString))
{
    var pooledConnectionString = DatabaseConnectionStringFactory.Build(defaultConnectionString, databasePoolingOptions);
    builder.Services.AddSingleton(sp =>
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(pooledConnectionString);
        return dataSourceBuilder.Build();
    });

    builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        options.UseNpgsql(
            sp.GetRequiredService<NpgsqlDataSource>(),
            b => b.CommandTimeout(databasePoolingOptions.CommandTimeoutSeconds)));
}

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(builder.Configuration["Redis:ConnectionString"] ?? "localhost");
    configuration.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services.AddSingleton<IMinioClient>(sp =>
    new MinioClient()
        .WithEndpoint(builder.Configuration["Minio:Endpoint"] ?? "minio:9000")
        .WithCredentials(builder.Configuration["Minio:AccessKey"] ?? "minioadmin", builder.Configuration["Minio:SecretKey"] ?? "minioadmin")
        .WithSSL(false)
        .Build());

builder.Services.AddHealthChecks()
    .AddCheck<SandboxHealthCheck>("sandbox", failureStatus: HealthStatus.Unhealthy);

if (!string.IsNullOrWhiteSpace(defaultConnectionString))
{
    builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("render-run-db");
}

var rateLimitOptions = builder.Configuration.GetSection("RenderRateLimit").Get<RenderRateLimitOptions>()
    ?? new RenderRateLimitOptions();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("render", context =>
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            clientIp,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimitOptions.PermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitOptions.WindowSeconds),
                QueueLimit = rateLimitOptions.QueueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    });
});

builder.Services.AddSingleton<INodeRenderService, SandboxRenderService>();
builder.Services.AddSingleton<RenderRunCachePruner>();
builder.Services.AddHttpClient("PageUiDiagnostics", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<RenderDiagnosticsOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.PageUiBaseUrl))
    {
        client.BaseAddress = new Uri(options.PageUiBaseUrl);
    }

    client.Timeout = TimeSpan.FromSeconds(10);
});
if (!string.IsNullOrWhiteSpace(defaultConnectionString))
{
    builder.Services.AddScoped<IRenderRunMetadataStore, RenderRunMetadataStore>();
}
else
{
    builder.Services.AddSingleton<IRenderRunMetadataStore, NullRenderRunMetadataStore>();
}

var serviceApiKey = builder.Configuration["ServiceAuth:ApiKey"] ?? builder.Configuration["ServiceAuth__ApiKey"];
var allowAnonymousInDevelopment = builder.Configuration.GetValue<bool>("ServiceAuth:AllowAnonymousInDevelopment");
if (string.IsNullOrWhiteSpace(serviceApiKey) &&
    !(builder.Environment.IsDevelopment() && allowAnonymousInDevelopment))
{
    throw new InvalidOperationException(
        "ServiceAuth:ApiKey is required. To bypass only in Development, set ServiceAuth:AllowAnonymousInDevelopment=true.");
}

var app = builder.Build();

var options = app.Services.GetRequiredService<IOptions<RenderOptions>>().Value;
var runsPath = RenderPathGuard.GetContainedRootPath(app.Environment.ContentRootPath, options.RunsDirectory);
if (!Directory.Exists(runsPath)) Directory.CreateDirectory(runsPath);
await app.Services.GetRequiredService<RenderRunCachePruner>().PruneIfDueAsync(CancellationToken.None);

var previewCsp = BuildPreviewCsp(app.Environment.IsDevelopment());

app.UseSerilogRequestLogging();
app.UseAuthorization();
app.UseResponseCompression();
app.UseRateLimiter();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/runs", out var remainingPath))
    {
        var segments = remainingPath.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (segments.Length >= 2)
        {
            var publicRunToken = segments[0];
            var assetPath = string.Join('/', segments.Skip(1));
            using var scope = app.Services.CreateScope();
            var metadataStore = scope.ServiceProvider.GetRequiredService<IRenderRunMetadataStore>();
            var run = await metadataStore.GetByPublicRunTokenAsync(publicRunToken, context.RequestAborted);
            if (run is not null && !string.IsNullOrWhiteSpace(run.RelativeRunPath))
            {
                context.Request.Path = $"/runs/{run.RelativeRunPath.Trim('/')}/{assetPath}";
            }
        }
    }

    if (!context.Request.Path.StartsWithSegments("/api/render-objects", StringComparison.OrdinalIgnoreCase) &&
        !context.Request.Path.StartsWithSegments("/api/render-form", StringComparison.OrdinalIgnoreCase) &&
        !context.Request.Path.StartsWithSegments("/api/runs", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    if (string.IsNullOrWhiteSpace(serviceApiKey))
    {
        if (app.Environment.IsDevelopment() && allowAnonymousInDevelopment)
        {
            await next();
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { error = "Render API key is not configured." });
        return;
    }

    var providedKey = context.Request.Headers["X-Render-Api-Key"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(providedKey) || !AreApiKeysEqual(providedKey, serviceApiKey))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
        return;
    }

    await next();
});

var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".js"] = "application/javascript";
contentTypeProvider.Mappings[".css"] = "text/css";
contentTypeProvider.Mappings[".html"] = "text/html";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(runsPath),
    RequestPath = "/runs",
    ContentTypeProvider = contentTypeProvider,
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        ctx.Context.Response.Headers["Pragma"] = "no-cache";
        ctx.Context.Response.Headers["Expires"] = "0";
        ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        // Removed to allow cross-origin iframe embedding:
        // ctx.Context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        ctx.Context.Response.Headers["Referrer-Policy"] = "no-referrer";
        ctx.Context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        ctx.Context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
        ctx.Context.Response.Headers["Content-Security-Policy"] = previewCsp;
    }
});

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

static bool AreApiKeysEqual(string provided, string expected)
{
    var providedBytes = Encoding.UTF8.GetBytes(provided.Trim());
    var expectedBytes = Encoding.UTF8.GetBytes(expected.Trim());

    return providedBytes.Length == expectedBytes.Length &&
           CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
}

static string BuildPreviewCsp(bool isDevelopment)
{
    var allowedStylesheetHosts = (Environment.GetEnvironmentVariable("PAGE_UI_ALLOWED_STYLESHEET_HOSTS") ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(host => host.ToLowerInvariant())
        .Where(host => host.Length > 0)
        .Append("fonts.googleapis.com")
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Select(host => $"https://{host}");
    var styleSources = string.Join(' ', allowedStylesheetHosts);
    var styleSrc = string.IsNullOrWhiteSpace(styleSources)
        ? "style-src 'self' 'unsafe-inline'; "
        : $"style-src 'self' 'unsafe-inline' {styleSources}; ";

    return "sandbox allow-scripts allow-same-origin; " +
           "default-src 'none'; " +
           "script-src 'self'; " +
           styleSrc +
           "img-src 'self' data: blob: https:; " +
           "font-src 'self' data: https:; " +
           "connect-src 'self'; " +
           "base-uri 'none'; " +
           "form-action 'none';";
    // Removed to allow cross-origin iframe embedding:
    // "frame-ancestors 'self'; " +
}
