using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Page.Ui.Infrastructure.Auth.Persistence;

#nullable disable

namespace Page.Ui.Infrastructure.Auth.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260309170000_ReshapeChatOwnership")]
    public partial class ReshapeChatOwnership : Migration
    {
        private const string AiBotUserId = "00000000-0000-0000-0000-000000000001";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerUserId",
                table: "Chats",
                type: "text",
                nullable: true);

            migrationBuilder.Sql($"""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "Chats" c
                        LEFT JOIN "ChatParticipants" cp
                            ON cp."ChatId" = c."Id"
                           AND cp."UserId" <> '{AiBotUserId}'
                        GROUP BY c."Id"
                        HAVING COUNT(cp."UserId") <> 1
                    ) THEN
                        RAISE EXCEPTION 'Chat reshape aborted: every chat must have exactly one non-AI participant.';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.Sql($"""
                UPDATE "Chats" c
                SET "OwnerUserId" = cp."UserId"
                FROM "ChatParticipants" cp
                WHERE cp."ChatId" = c."Id"
                  AND cp."UserId" <> '{AiBotUserId}';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "OwnerUserId",
                table: "Chats",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chats_OwnerUserId",
                table: "Chats",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_OwnerUserId_CreatedAt",
                table: "Chats",
                columns: new[] { "OwnerUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Chats_OwnerUserId_UpdatedAt",
                table: "Chats",
                columns: new[] { "OwnerUserId", "UpdatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_AspNetUsers_OwnerUserId",
                table: "Chats",
                column: "OwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropTable(
                name: "ChatParticipants");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatParticipants",
                columns: table => new
                {
                    ChatId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastReadAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastReadMessageId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatParticipants", x => new { x.ChatId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ChatParticipants_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatParticipants_Chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatParticipants_UserId",
                table: "ChatParticipants",
                column: "UserId");

            migrationBuilder.Sql($"""
                INSERT INTO "ChatParticipants" ("ChatId", "UserId", "JoinedAt", "LastReadAt", "LastReadMessageId")
                SELECT c."Id", c."OwnerUserId", c."CreatedAt", NULL, NULL
                FROM "Chats" c;
                """);

            migrationBuilder.Sql($"""
                INSERT INTO "ChatParticipants" ("ChatId", "UserId", "JoinedAt", "LastReadAt", "LastReadMessageId")
                SELECT c."Id", '{AiBotUserId}', c."CreatedAt", NULL, NULL
                FROM "Chats" c
                ON CONFLICT ("ChatId", "UserId") DO NOTHING;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Chats_AspNetUsers_OwnerUserId",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_Chats_OwnerUserId",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_Chats_OwnerUserId_CreatedAt",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_Chats_OwnerUserId_UpdatedAt",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Chats");
        }
    }
}
