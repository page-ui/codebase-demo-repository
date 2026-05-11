using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Page.Ui.Infrastructure.Auth.Persistence;

#nullable disable

namespace Page.Ui.Infrastructure.Auth.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260424131500_AddAiRunVersioningAndMessageMetadata")]
    public partial class AddAiRunVersioningAndMessageMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientRequestId",
                table: "Messages",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Messages",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Messages_Title_Length",
                table: "Messages",
                sql: "char_length(\"Title\") <= 160");

            migrationBuilder.CreateTable(
                name: "AiRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TriggerMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModelId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    SupersededByRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ManifestObjectKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    FinalPreviewUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FailureMessageSafe = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiRuns", x => x.Id);
                    table.CheckConstraint("CK_AiRuns_Title_Length", "char_length(\"Title\") <= 160");
                    table.ForeignKey(
                        name: "FK_AiRuns_AiRuns_SupersededByRunId",
                        column: x => x.SupersededByRunId,
                        principalTable: "AiRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AiRuns_Chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AiRuns_Messages_TriggerMessageId",
                        column: x => x.TriggerMessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AiRunFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    StoredFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiRunFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiRunFiles_AiRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "AiRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiRunFiles_RunId_Role",
                table: "AiRunFiles",
                columns: new[] { "RunId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_AiRuns_ChatId_CreatedAt",
                table: "AiRuns",
                columns: new[] { "ChatId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiRuns_ChatId_IsCurrent",
                table: "AiRuns",
                columns: new[] { "ChatId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_AiRuns_OwnerUserId_CreatedAt",
                table: "AiRuns",
                columns: new[] { "OwnerUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AiRuns_SupersededByRunId",
                table: "AiRuns",
                column: "SupersededByRunId");

            migrationBuilder.CreateIndex(
                name: "IX_AiRuns_TriggerMessageId",
                table: "AiRuns",
                column: "TriggerMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_AiRuns_VersionId",
                table: "AiRuns",
                column: "VersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChatId_SenderId_ClientRequestId",
                table: "Messages",
                columns: new[] { "ChatId", "SenderId", "ClientRequestId" },
                unique: true,
                filter: "\"ClientRequestId\" IS NOT NULL");

            migrationBuilder.CreateTable(
                name: "RenderRuns",
                columns: table => new
                {
                    RunId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PublicRunToken = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    UserStorageKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ChatId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChatKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    VersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastAccessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RelativeRunPath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    PreviewUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ErrorSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SourceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RenderRuns", x => x.RunId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RenderRuns_ChatId_CreatedAtUtc",
                table: "RenderRuns",
                columns: new[] { "ChatId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RenderRuns_MessageId",
                table: "RenderRuns",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_RenderRuns_PublicRunToken",
                table: "RenderRuns",
                column: "PublicRunToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RenderRuns_Status_CreatedAtUtc",
                table: "RenderRuns",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RenderRuns_UserId_CreatedAtUtc",
                table: "RenderRuns",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiRunFiles");

            migrationBuilder.DropTable(
                name: "AiRuns");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ChatId_SenderId_ClientRequestId",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "RenderRuns");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Messages_Title_Length",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Messages");
        }
    }
}
