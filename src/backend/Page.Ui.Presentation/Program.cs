using System.IO.Compression;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotChocolate.Execution.Options;
using HotChocolate.Types;
using Microsoft.AspNetCore.Authorization;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Minio;
using Npgsql;
using Page.Ui.Application.Auth.Interfaces;
using Page.Ui.Application.Chat.Services;
using Page.Ui.Application.Chat.Configuration;
using Page.Ui.Application.Common.Interfaces;
using Page.Ui.Domain.Auth.Entities;
using Page.Ui.Infrastructure.Auth.Persistence;
using Page.Ui.Infrastructure.Auth.Services;
using Page.Ui.Infrastructure.Configuration;
using Page.Ui.Infrastructure.Chat.Services;
using Page.Ui.Presentation.Auth.Consumers;
using Page.Ui.Presentation.Auth.GraphQl.Mutations;
using Page.Ui.Presentation.Auth.Services;
using Page.Ui.Presentation.Chat.Consumers;
using Page.Ui.Presentation.Chat.GraphQl;
using Page.Ui.Presentation.Chat.GraphQl.Mutations;
using Page.Ui.Presentation.Chat.GraphQl.Queries;
using Page.Ui.Presentation.Chat.GraphQl.Subscriptions;
using Page.Ui.Presentation.Chat.GraphQl.Views;
using Page.Ui.Presentation.Chat.Hubs;
using Page.Ui.Presentation.Chat.Time;
using Page.Ui.Presentation.Health;
using Page.Ui.Presentation.Health.GraphQl.Queries;
using Serilog;
using StackExchange.Redis;

const string BearerSelectorScheme = "BearerSelector";
const string InternalServiceScheme = "InternalService";
const string UserApiPolicy = "UserApiPolicy";
const string AiApiPolicy = "AiApiPolicy";
const string InternalAiApiPolicy = "InternalAiApiPolicy";

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Service", "Page.Ui.Presentation");
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
var databasePoolingOptions = builder.Configuration.GetSection("Database:Pooling").Get<DatabasePoolingOptions>() ?? new();
var pooledConnectionString = DatabaseConnectionStringFactory.Build(defaultConnectionString, databasePoolingOptions);
var allowedCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? [];
var allowAnyCorsOrigin = builder.Environment.IsDevelopment() &&
    builder.Configuration.GetValue("Cors:AllowAnyOriginInDevelopment", true);
var applyMigrationsOnStartup = builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false);

builder.Services.AddSingleton(sp =>
{
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(pooledConnectionString);
    return dataSourceBuilder.Build();
});

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
    options.UseNpgsql(
        sp.GetRequiredService<NpgsqlDataSource>(),
        b =>
        {
            b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            b.CommandTimeout(databasePoolingOptions.CommandTimeoutSeconds);
        }));

builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>()
    .AddRedis(builder.Configuration["Redis:ConnectionString"] ?? "localhost")
    .AddRabbitMQ(rabbitConnectionString: $"amqp://{builder.Configuration["RabbitMq:Username"] ?? "guest"}:{builder.Configuration["RabbitMq:Password"] ?? "guest"}@{builder.Configuration["RabbitMq:Host"] ?? "localhost"}")
    .AddCheck<MinioHealthCheck>("minio")
    .AddCheck<SmtpHealthCheck>("smtp");

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

builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddSingleton<ChatClientTimeConverter>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthMutationWorkflow>();
builder.Services.AddSingleton<IRsaKeyService, RsaKeyService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowedCorsOrigins.Length > 0)
        {
            policy.WithOrigins(allowedCorsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        if (allowAnyCorsOrigin)
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/graphql-response+json"]);
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration["Redis:ConnectionString"] ?? "localhost", options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("PageUiChat");
    });

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AuthEmailRequestedConsumer>(typeof(AuthEmailRequestedConsumerDefinition));
    x.AddConsumer<ChatMessageCreatedConsumer>(typeof(ChatMessageCreatedConsumerDefinition));
    x.AddConsumer<AiResponseMessageGeneratedConsumer>(typeof(AiResponseMessageGeneratedConsumerDefinition));

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

builder.Services.AddGraphQLServer()
    .AddQueryType(d => d.Name("Query"))
        .AddTypeExtension<HealthQuery>()
        .AddTypeExtension<ChatQueries>()
    .AddMutationType(d => d.Name("Mutation"))
        .AddTypeExtension<AuthMutations>()
        .AddTypeExtension<ChatMutations>()
    .AddSubscriptionType(d => d.Name("Subscription"))
        .AddTypeExtension<ChatSubscriptions>()
    .AddType(new ObjectType<ChatView>(d =>
    {
        d.Field(c => c.CreatedAt)
            .Resolve(ctx => ctx.Service<ChatClientTimeConverter>().Convert(ctx.Parent<ChatView>().CreatedAt));
        d.Field(c => c.UpdatedAt)
            .Resolve(ctx => ctx.Service<ChatClientTimeConverter>().Convert(ctx.Parent<ChatView>().UpdatedAt));
    }))
    .AddType(new ObjectType<MessageView>(d =>
    {
        d.Field(m => m.CreatedAt)
            .Resolve(ctx => ctx.Service<ChatClientTimeConverter>().Convert(ctx.Parent<MessageView>().CreatedAt));
        d.Field(m => m.UpdatedAt)
            .Resolve(ctx => ctx.Service<ChatClientTimeConverter>().Convert(ctx.Parent<MessageView>().UpdatedAt));
    }))
    .AddAuthorization()
    .AddSocketSessionInterceptor<ChatSocketSessionInterceptor>()
    .AddRedisSubscriptions(sp => sp.GetRequiredService<IConnectionMultiplexer>())
    .AddFiltering()
    .AddSorting()
    .AddProjections()
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 5_000;
        options.MaxTypeCost = 5_000;
        options.EnforceCostLimits = true;
        options.Filtering.DefaultFilterArgumentCost = 5.0;
        options.Filtering.DefaultFilterOperationCost = 5.0;
        options.Filtering.DefaultExpensiveFilterOperationCost = 10.0;
        options.Sorting.DefaultSortArgumentCost = 5.0;
        options.Sorting.DefaultSortOperationCost = 5.0;
    })
    .AddMaxExecutionDepthRule(100);

builder.Services.Configure<InternalServiceJwtOptions>(builder.Configuration.GetSection("InternalServiceJwt"));
var internalServiceJwtOptions = builder.Configuration.GetSection("InternalServiceJwt").Get<InternalServiceJwtOptions>() ?? new();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = BearerSelectorScheme;
    options.DefaultChallengeScheme = BearerSelectorScheme;
})
.AddPolicyScheme(BearerSelectorScheme, "User or internal service bearer", options =>
{
    options.ForwardDefaultSelector = context => SelectBearerScheme(context, internalServiceJwtOptions);
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (!string.IsNullOrWhiteSpace(context.Token))
            {
                return Task.CompletedTask;
            }

            var path = context.HttpContext.Request.Path;
            if (path.StartsWithSegments("/graphql") || path.StartsWithSegments("/hubs/chat"))
            {
                var accessToken = context.Request.Query["access_token"].ToString();

                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    if (accessToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        accessToken = accessToken["Bearer ".Length..].Trim();
                    }
                    context.Token = accessToken;
                }
            }

            return Task.CompletedTask;
        }
    };
})
.AddJwtBearer(InternalServiceScheme, options =>
{
    options.MapInboundClaims = false;
});

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IRsaKeyService, IConfiguration>((options, rsaKeyService, configuration) =>
    {
        var rsa = rsaKeyService.GetPublicKey();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidateIssuer = true,
            ValidIssuers = new[] { "AuthService", "PageUi", configuration["JWT:Issuer"], configuration["JWT__Issuer"] }.Where(x => !string.IsNullOrEmpty(x)).ToArray(),
            ValidateAudience = true,
            ValidAudiences = new[] { "AuthService", "PageUiUser", "PageUi", configuration["JWT:Audience"], configuration["JWT__Audience"] }.Where(x => !string.IsNullOrEmpty(x)).ToArray(),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services
    .AddOptions<JwtBearerOptions>(InternalServiceScheme)
    .Configure<IRsaKeyService, IOptions<InternalServiceJwtOptions>>((options, rsaKeyService, internalOptions) =>
    {
        var rsa = rsaKeyService.GetPublicKey();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidateIssuer = true,
            ValidIssuer = internalOptions.Value.Issuer,
            ValidateAudience = true,
            ValidAudience = internalOptions.Value.Audience,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (!string.IsNullOrWhiteSpace(context.Token))
                {
                    return Task.CompletedTask;
                }

                var path = context.HttpContext.Request.Path;
                if (path.StartsWithSegments("/graphql") || path.StartsWithSegments("/hubs/chat"))
                {
                    var accessToken = context.Request.Query["access_token"].ToString();

                    if (!string.IsNullOrWhiteSpace(accessToken))
                    {
                        if (accessToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            accessToken = accessToken["Bearer ".Length..].Trim();
                        }
                        context.Token = accessToken;
                    }
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireAssertion(IsUserApiPrincipal)
        .Build();

    options.AddPolicy(UserApiPolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(IsUserApiPrincipal);
    });

    options.AddPolicy(AiApiPolicy, policy =>
    {
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.AuthenticationSchemes.Add(InternalServiceScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(IsAiApiPrincipal);
    });

    options.AddPolicy(InternalAiApiPolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(IsInternalAiApiPrincipal);
    });
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.MaxDepth = 64;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseForwardedHeaders();
app.UseSerilogRequestLogging();
app.UseResponseCompression();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();

app.MapGraphQL();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHealthChecks("/health");

if (applyMigrationsOnStartup)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    db.Database.Migrate();
    DatabaseStartupSchemaVerifier.VerifyRequiredChatSchema(db);
}

static string SelectBearerScheme(HttpContext context, InternalServiceJwtOptions internalOptions)
{
    var token = ReadBearerToken(context);
    if (LooksLikeInternalServiceToken(token, internalOptions))
    {
        return InternalServiceScheme;
    }

    return JwtBearerDefaults.AuthenticationScheme;
}

static string? ReadBearerToken(HttpContext context)
{
    var authorization = context.Request.Headers.Authorization.ToString();
    if (!string.IsNullOrWhiteSpace(authorization))
    {
        return authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : authorization.Trim();
    }

    var path = context.Request.Path;
    if (path.StartsWithSegments("/graphql") || path.StartsWithSegments("/hubs/chat"))
    {
        var accessToken = context.Request.Query["access_token"].ToString();
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            return accessToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? accessToken["Bearer ".Length..].Trim()
                : accessToken.Trim();
        }
    }

    return null;
}

static bool LooksLikeInternalServiceToken(string? token, InternalServiceJwtOptions internalOptions)
{
    if (string.IsNullOrWhiteSpace(token))
    {
        return false;
    }

    var handler = new JwtSecurityTokenHandler();
    if (!handler.CanReadToken(token))
    {
        return false;
    }

    var jwt = handler.ReadJwtToken(token);
    return string.Equals(jwt.Issuer, internalOptions.Issuer, StringComparison.Ordinal) &&
           jwt.Audiences.Contains(internalOptions.Audience, StringComparer.Ordinal);
}

static bool IsUserApiPrincipal(AuthorizationHandlerContext context)
{
    return !string.Equals(context.User.FindFirstValue(JwtRegisteredClaimNames.Sub), "worker-ai", StringComparison.Ordinal);
}

static bool IsAiApiPrincipal(AuthorizationHandlerContext context)
{
    if (!string.Equals(context.User.FindFirstValue(JwtRegisteredClaimNames.Sub), "worker-ai", StringComparison.Ordinal))
    {
        return true;
    }

    return !string.IsNullOrWhiteSpace(context.User.FindFirstValue("user_id")) &&
           !string.IsNullOrWhiteSpace(context.User.FindFirstValue("chat_id")) &&
           !string.IsNullOrWhiteSpace(context.User.FindFirstValue("message_id"));
}

static bool IsInternalAiApiPrincipal(AuthorizationHandlerContext context)
{
    return string.Equals(context.User.FindFirstValue(JwtRegisteredClaimNames.Sub), "worker-ai", StringComparison.Ordinal) &&
           !string.IsNullOrWhiteSpace(context.User.FindFirstValue("user_id")) &&
           !string.IsNullOrWhiteSpace(context.User.FindFirstValue("chat_id")) &&
           !string.IsNullOrWhiteSpace(context.User.FindFirstValue("message_id"));
}

app.Run();
