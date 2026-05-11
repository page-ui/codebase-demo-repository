using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Page.Ui.Infrastructure.Auth.Persistence;

#nullable disable

namespace Page.Ui.Infrastructure.Auth.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260304001000_DropLegacyRefreshTokenTokenColumnIfExists")]
    public partial class DropLegacyRefreshTokenTokenColumnIfExists : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "RefreshTokens"
                DROP COLUMN IF EXISTS "Token";
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "RefreshTokens"
                ADD COLUMN IF NOT EXISTS "Token" text NOT NULL DEFAULT '';
                """);
        }
    }
}
