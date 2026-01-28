using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Swoosh.Api.Migrations
{
    /// <inheritdoc />
    public partial class UserEncryptionSalt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptionSalt",
                table: "Users",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptionSalt",
                table: "Users");
        }
    }
}
