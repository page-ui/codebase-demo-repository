using Npgsql;

namespace Page.Ui.Infrastructure.Configuration;

public sealed class DatabasePoolingOptions
{
    public bool Enabled { get; set; } = true;
    public int MinimumPoolSize { get; set; } = 5;
    public int MaximumPoolSize { get; set; } = 60;
    public int TimeoutSeconds { get; set; } = 15;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public int ConnectionIdleLifetimeSeconds { get; set; } = 300;
    public int ConnectionPruningIntervalSeconds { get; set; } = 10;
}

public static class DatabaseConnectionStringFactory
{
    public static string Build(string connectionString, DatabasePoolingOptions? options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        options ??= new DatabasePoolingOptions();
        var timeoutSeconds = Math.Max(1, options.TimeoutSeconds);
        var commandTimeoutSeconds = Math.Max(1, options.CommandTimeoutSeconds);
        var idleLifetimeSeconds = Math.Max(1, options.ConnectionIdleLifetimeSeconds);
        var pruningIntervalSeconds = Math.Max(1, options.ConnectionPruningIntervalSeconds);
        var minimumPoolSize = Math.Max(0, options.MinimumPoolSize);
        var maximumPoolSize = Math.Max(minimumPoolSize, options.MaximumPoolSize);

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Timeout = timeoutSeconds,
            CommandTimeout = commandTimeoutSeconds,
            Pooling = options.Enabled
        };

        if (options.Enabled)
        {
            builder.MinPoolSize = minimumPoolSize;
            builder.MaxPoolSize = maximumPoolSize;
            builder.ConnectionIdleLifetime = idleLifetimeSeconds;
            builder.ConnectionPruningInterval = pruningIntervalSeconds;
        }

        return builder.ConnectionString;
    }
}
