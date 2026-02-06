using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Swoosh.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEncryptedPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedPriority",
                table: "Tasks",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedPriority",
                table: "Tasks");
        }
    }
}
