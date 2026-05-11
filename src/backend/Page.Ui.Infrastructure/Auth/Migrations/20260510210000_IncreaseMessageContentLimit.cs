using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Page.Ui.Infrastructure.Auth.Persistence;

#nullable disable

namespace Page.Ui.Infrastructure.Auth.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260510210000_IncreaseMessageContentLimit")]
    public partial class IncreaseMessageContentLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Messages_Content_Length",
                table: "Messages");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Messages_Content_Length",
                table: "Messages",
                sql: "char_length(\"Content\") BETWEEN 1 AND 10000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Messages_Content_Length",
                table: "Messages");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Messages_Content_Length",
                table: "Messages",
                sql: "char_length(\"Content\") BETWEEN 1 AND 4000");
        }
    }
}
