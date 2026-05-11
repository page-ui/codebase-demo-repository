using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Page.Ui.Infrastructure.Auth.Persistence;

#nullable disable

namespace Page.Ui.Infrastructure.Auth.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260307162000_AddPerformanceIndexesAndChatSearch")]
    public partial class AddPerformanceIndexesAndChatSearch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS pg_trgm;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetCodes_Email",
                table: "PasswordResetCodes",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_PendingRegistrations_Email",
                table: "PendingRegistrations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_HashedToken",
                table: "RefreshTokens",
                column: "HashedToken",
                unique: true);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_Chats_Name_Trgm"
                ON "Chats"
                USING GIN ("Name" gin_trgm_ops)
                WHERE "Name" IS NOT NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_Chats_Name_Trgm";
                """);

            migrationBuilder.DropIndex(
                name: "IX_PasswordResetCodes_Email",
                table: "PasswordResetCodes");

            migrationBuilder.DropIndex(
                name: "IX_PendingRegistrations_Email",
                table: "PendingRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_HashedToken",
                table: "RefreshTokens");

            migrationBuilder.Sql("""
                DROP EXTENSION IF EXISTS pg_trgm;
                """);
        }
    }
}
