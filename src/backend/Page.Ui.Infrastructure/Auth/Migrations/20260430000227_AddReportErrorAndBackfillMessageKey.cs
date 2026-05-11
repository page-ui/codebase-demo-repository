using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Page.Ui.Infrastructure.Auth.Migrations
{
    /// <inheritdoc />
    public partial class AddReportErrorAndBackfillMessageKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MessageKey",
                table: "Messages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Messages\" SET \"MessageKey\" = substring(replace(\"Id\"::text, '-', ''), 1, 24) WHERE \"MessageKey\" IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "MessageKey",
                table: "Messages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClientErrors",
                table: "AiRuns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientLogs",
                table: "AiRuns",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0000-000000000001",
                column: "ConcurrencyStamp",
                value: "aed85bbe-bb5c-4a22-80de-d62125bd7845");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_MessageKey",
                table: "Messages",
                column: "MessageKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_MessageKey",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MessageKey",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ClientErrors",
                table: "AiRuns");

            migrationBuilder.DropColumn(
                name: "ClientLogs",
                table: "AiRuns");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "00000000-0000-0000-0000-000000000001",
                column: "ConcurrencyStamp",
                value: "eb7daf1d-8c44-4681-a57c-a4351cca3b94");
        }
    }
}
