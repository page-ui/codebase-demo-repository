using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Page.Ui.Infrastructure.Auth.Persistence;

#nullable disable

namespace Page.Ui.Infrastructure.Auth.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260303235900_MakeCreateChatServerAuthoritative")]
    public partial class MakeCreateChatServerAuthoritative : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChatKey",
                table: "Chats",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelId",
                table: "Chats",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "assistant-default");

            migrationBuilder.AddColumn<string>(
                name: "SystemPrompt",
                table: "Chats",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServerGeneratedId",
                table: "Messages",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Chats"
                SET "ChatKey" = REPLACE("Id"::text, '-', '')
                WHERE "ChatKey" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ChatKey",
                table: "Chats",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Chats_Name_MaxLength",
                table: "Chats",
                sql: "\"Name\" IS NULL OR char_length(\"Name\") <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Chats_SystemPrompt_MaxLength",
                table: "Chats",
                sql: "\"SystemPrompt\" IS NULL OR char_length(\"SystemPrompt\") <= 4000");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Messages_Content_Length",
                table: "Messages",
                sql: "char_length(\"Content\") BETWEEN 1 AND 4000");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_ChatKey",
                table: "Chats",
                column: "ChatKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Chats_ChatKey",
                table: "Chats");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Chats_Name_MaxLength",
                table: "Chats");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Chats_SystemPrompt_MaxLength",
                table: "Chats");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Messages_Content_Length",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ChatKey",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "ModelId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "SystemPrompt",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "ServerGeneratedId",
                table: "Messages");
        }
    }
}
