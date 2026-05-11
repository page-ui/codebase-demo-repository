using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Page.Ui.Infrastructure.Auth.Persistence;
using Page.Ui.Backend.Tests.TestSupport;

namespace Page.Ui.Backend.Tests.Auth;

public class MigrationConfigurationTests
{
    [Fact]
    public void AllMigrationTypesExposeEfMetadataAndAreDiscoverable()
    {
        var migrationTypes = typeof(ApplicationDbContext).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(Migration).IsAssignableFrom(t) && t.Namespace == "Page.Ui.Infrastructure.Auth.Migrations")
            .OrderBy(t => t.Name)
            .ToArray();

        Assert.NotEmpty(migrationTypes);

        var migrationIds = new List<string>();

        foreach (var migrationType in migrationTypes)
        {
            var dbContextAttribute = migrationType.GetCustomAttribute<DbContextAttribute>();
            Assert.NotNull(dbContextAttribute);
            Assert.Equal(typeof(ApplicationDbContext), dbContextAttribute!.ContextType);

            var migrationAttribute = migrationType.GetCustomAttribute<MigrationAttribute>();
            Assert.NotNull(migrationAttribute);
            migrationIds.Add(migrationAttribute!.Id);
        }

        using var db = CreateNpgsqlContext();
        var migrationsAssembly = db.GetService<IMigrationsAssembly>();
        var discoveredIds = migrationsAssembly.Migrations.Keys.OrderBy(id => id).ToArray();

        Assert.Equal(migrationIds.OrderBy(id => id), discoveredIds);
        Assert.Contains("20260424131500_AddAiRunVersioningAndMessageMetadata", discoveredIds);
    }

    [Fact]
    public void StartupSchemaVerifierSkipsNonNpgsqlProviders()
    {
        using var db = TestDbFactory.CreateContext();

        DatabaseStartupSchemaVerifier.VerifyRequiredChatSchema(db);
    }

    private static ApplicationDbContext CreateNpgsqlContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=pageuidb;Username=postgres;Password=password",
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;

        return new ApplicationDbContext(options);
    }
}
