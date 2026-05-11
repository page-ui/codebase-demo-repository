using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Page.Ui.Infrastructure.Auth.Persistence;

public static class DatabaseStartupSchemaVerifier
{
    private static readonly string[] RequiredTables =
    [
        "__EFMigrationsHistory",
        "AiRuns",
        "AiRunFiles",
        "RenderRuns"
    ];

    private static readonly (string Table, string Column)[] RequiredColumns =
    [
        ("Messages", "ClientRequestId"),
        ("Messages", "Title"),
        ("Messages", "IsQuestion")
    ];

    public static void VerifyRequiredChatSchema(ApplicationDbContext db)
    {
        if (!string.Equals(db.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal))
        {
            return;
        }

        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            foreach (var table in RequiredTables)
            {
                if (!TableExists(connection, table))
                {
                    throw new InvalidOperationException(
                        $"Database startup verification failed: required table '{table}' is missing after migrations were applied.");
                }
            }

            foreach (var (table, column) in RequiredColumns)
            {
                if (!ColumnExists(connection, table, column))
                {
                    throw new InvalidOperationException(
                        $"Database startup verification failed: required column '{table}.{column}' is missing after migrations were applied.");
                }
            }
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }

    private static bool TableExists(DbConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = 'public' AND table_name = @tableName;
            """;
        AddParameter(command, "@tableName", tableName);
        return ExecuteCount(command) > 0;
    }

    private static bool ColumnExists(DbConnection connection, string tableName, string columnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = @tableName
              AND column_name = @columnName;
            """;
        AddParameter(command, "@tableName", tableName);
        AddParameter(command, "@columnName", columnName);
        return ExecuteCount(command) > 0;
    }

    private static int ExecuteCount(DbCommand command)
    {
        var result = command.ExecuteScalar();
        return Convert.ToInt32(result);
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
