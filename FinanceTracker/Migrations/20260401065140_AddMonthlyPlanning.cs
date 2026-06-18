using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonthlyPlannings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    PaidDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    RecurringDay = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyPlannings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyPlannings_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPlannings_CategoryId",
                table: "MonthlyPlannings",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyPlannings");
        }
    }
}
