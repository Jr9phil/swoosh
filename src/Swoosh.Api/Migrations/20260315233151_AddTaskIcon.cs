using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Swoosh.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskIcon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedIcon",
                table: "Tasks",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedIcon",
                table: "Tasks");
        }
    }
}
