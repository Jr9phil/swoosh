using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Swoosh.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakePriorityNotNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EncryptedPriority",
                table: "Tasks",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
