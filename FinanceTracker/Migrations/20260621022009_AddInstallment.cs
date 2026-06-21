using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InstallmentId",
                table: "MonthlyPlannings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Installments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlafondAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MonthlyAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenorMonths = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Installments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Installments_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyPlannings_InstallmentId",
                table: "MonthlyPlannings",
                column: "InstallmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Installments_CategoryId",
                table: "Installments",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyPlannings_Installments_InstallmentId",
                table: "MonthlyPlannings",
                column: "InstallmentId",
                principalTable: "Installments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MonthlyPlannings_Installments_InstallmentId",
                table: "MonthlyPlannings");

            migrationBuilder.DropTable(
                name: "Installments");

            migrationBuilder.DropIndex(
                name: "IX_MonthlyPlannings_InstallmentId",
                table: "MonthlyPlannings");

            migrationBuilder.DropColumn(
                name: "InstallmentId",
                table: "MonthlyPlannings");
        }
    }
}
