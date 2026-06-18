using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalCompletionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Goals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "Goals",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "Goals");
        }
    }
}
