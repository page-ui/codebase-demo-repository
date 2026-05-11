using MassTransit;
using Microsoft.EntityFrameworkCore;
using Minio;
using Page.Ui.Worker.Ai.Health;
using Page.Ui.Application.Common.Interfaces;
using Page.Ui.Infrastructure.Auth.Persistence;
using Page.Ui.Infrastructure.Auth.Services;
using Page.Ui.Infrastructure.Configuration;
using Page.Ui.Application.Chat.Configuration;
using Page.Ui.Worker.Ai.Configuration;
using Page.Ui.Worker.Ai.Consumers;
using Page.Ui.Worker.Ai.Services;
using Npgsql;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "Page.Ui.Worker.Ai");
});

var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
var databasePoolingOptions = builder.Configuration.GetSection("Database:Pooling").Get<DatabasePoolingOptions>() ?? new();
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

builder.Services.Configure<AiModelApiOptions>(builder.Configuration.GetSection("AiModelApi"));
builder.Services.Configure<InternalServiceJwtOptions>(builder.Configuration.GetSection("InternalServiceJwt"));

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
    .AddDbContextCheck<ApplicationDbContext>()
    .AddRedis(builder.Configuration["Redis:ConnectionString"] ?? "localhost")
    .AddRabbitMQ(rabbitConnectionString: $"amqp://{builder.Configuration["RabbitMq:Username"] ?? "guest"}:{builder.Configuration["RabbitMq:Password"] ?? "guest"}@{builder.Configuration["RabbitMq:Host"] ?? "localhost"}")
    .AddCheck<RenderServiceHealthCheck>("svelte-render");

builder.Services.AddHttpClient("SvelteRender", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["SvelteRender:BaseUrl"] ?? builder.Configuration["SvelteRender__BaseUrl"] ?? "http://svelte-render:8080");
    client.Timeout = TimeSpan.FromSeconds(20);
    var renderApiKey = builder.Configuration["SvelteRender:ApiKey"] ?? builder.Configuration["SvelteRender__ApiKey"];
    if (!string.IsNullOrWhiteSpace(renderApiKey))
    {
        client.DefaultRequestHeaders.Add("X-Render-Api-Key", renderApiKey);
    }
});

builder.Services.AddHttpClient("AiModelApi", (sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiModelApiOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl);
    }

    client.Timeout = TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds));
});

builder.Services.AddSingleton<IRsaKeyService, RsaKeyService>();
builder.Services.AddScoped<IAiContextLoader, AiContextLoader>();
builder.Services.AddScoped<IAiModelClient, AiModelClient>();
builder.Services.AddScoped<IAiRunStorageService, AiRunStorageService>();
builder.Services.AddSingleton<IInternalServiceJwtProvider, Page.Ui.Infrastructure.Auth.Services.InternalServiceJwtProvider>();
builder.Services.AddSingleton<IThinkingMessageProvider, ThinkingMessageProvider>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ChatMessageCreatedConsumer>(typeof(ChatMessageCreatedConsumerDefinition));
    x.AddConsumer<AiRunRenderTriggerConsumer>();
    x.AddConsumer<RenderErrorReportedConsumer>();

    x.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();
app.UseSerilogRequestLogging();
app.MapHealthChecks("/health");
app.Run();
