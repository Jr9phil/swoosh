using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Swoosh.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringTasksNoteCardsReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NoteCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedTitle = table.Column<string>(type: "text", nullable: false),
                    EncryptedBody = table.Column<string>(type: "text", nullable: false),
                    EncryptedPositionX = table.Column<string>(type: "text", nullable: false),
                    EncryptedPositionY = table.Column<string>(type: "text", nullable: false),
                    KeyVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteCards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecurringTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedTitle = table.Column<string>(type: "text", nullable: false),
                    EncryptedNotes = table.Column<string>(type: "text", nullable: false),
                    EncryptedRecurrenceType = table.Column<string>(type: "text", nullable: false),
                    EncryptedRecurrenceInterval = table.Column<string>(type: "text", nullable: true),
                    EncryptedIsActive = table.Column<string>(type: "text", nullable: false),
                    KeyVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedTitle = table.Column<string>(type: "text", nullable: false),
                    EncryptedNotes = table.Column<string>(type: "text", nullable: false),
                    EncryptedRemindAt = table.Column<string>(type: "text", nullable: false),
                    EncryptedIsCompleted = table.Column<string>(type: "text", nullable: false),
                    KeyVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NoteCards_UserId",
                table: "NoteCards",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTasks_UserId",
                table: "RecurringTasks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_UserId",
                table: "Reminders",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NoteCards");

            migrationBuilder.DropTable(
                name: "RecurringTasks");

            migrationBuilder.DropTable(
                name: "Reminders");
        }
    }
}
