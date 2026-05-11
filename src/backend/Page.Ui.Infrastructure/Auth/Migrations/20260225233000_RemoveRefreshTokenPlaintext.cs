using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Page.Ui.Infrastructure.Auth.Persistence;

#nullable disable

namespace Page.Ui.Infrastructure.Auth.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260225233000_RemoveRefreshTokenPlaintext")]
    public partial class RemoveRefreshTokenPlaintext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "RefreshTokens"
                DROP COLUMN IF EXISTS "Token";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "RefreshTokens"
                ADD COLUMN IF NOT EXISTS "Token" text NOT NULL DEFAULT '';
                """);
        }
    }
}
