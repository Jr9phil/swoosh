using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Swoosh.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringTaskFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncryptedIcon",
                table: "RecurringTasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedPinned",
                table: "RecurringTasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedPriority",
                table: "RecurringTasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedRating",
                table: "RecurringTasks",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedIcon",
                table: "RecurringTasks");

            migrationBuilder.DropColumn(
                name: "EncryptedPinned",
                table: "RecurringTasks");

            migrationBuilder.DropColumn(
                name: "EncryptedPriority",
                table: "RecurringTasks");

            migrationBuilder.DropColumn(
                name: "EncryptedRating",
                table: "RecurringTasks");
        }
    }
}
