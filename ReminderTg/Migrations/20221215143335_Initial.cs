﻿using System;
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
                name: "Reminder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    ReminderTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ReminderDays = table.Column<int[]>(type: "integer[]", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    IsSave = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminder", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reminder");
        }
    }
}
