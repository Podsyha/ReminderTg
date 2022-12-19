using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReminderTg.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OnceReminder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    ReminderDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeZone = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    IsSave = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnceReminder", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RepeatReminder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    ReminderTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    ReminderDays = table.Column<int[]>(type: "integer[]", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    IsSave = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepeatReminder", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OnceReminder");

            migrationBuilder.DropTable(
                name: "RepeatReminder");
        }
    }
}
