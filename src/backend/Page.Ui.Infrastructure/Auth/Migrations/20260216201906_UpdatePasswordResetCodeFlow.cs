using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Page.Ui.Infrastructure.Auth.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePasswordResetCodeFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "PasswordResetCodes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TempToken",
                table: "PasswordResetCodes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "PasswordResetCodes");

            migrationBuilder.DropColumn(
                name: "TempToken",
                table: "PasswordResetCodes");
        }
    }
}
