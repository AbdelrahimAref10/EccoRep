using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addnewColumnToOderTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VO_Vehicle_CreatedThisMonth",
                table: "VO_Vehicle");

            migrationBuilder.DropIndex(
                name: "IX_VO_Vehicle_VehicleCode",
                table: "VO_Vehicle");

            migrationBuilder.DropColumn(
                name: "CreatedThisMonth",
                table: "VO_Vehicle");

            migrationBuilder.AddColumn<bool>(
                name: "MoneyRefunded",
                table: "VO_Order",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PreviousDebt",
                table: "VO_Order",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MoneyRefunded",
                table: "VO_Order");

            migrationBuilder.DropColumn(
                name: "PreviousDebt",
                table: "VO_Order");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedThisMonth",
                table: "VO_Vehicle",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VO_Vehicle_CreatedThisMonth",
                table: "VO_Vehicle",
                column: "CreatedThisMonth");

            migrationBuilder.CreateIndex(
                name: "IX_VO_Vehicle_VehicleCode",
                table: "VO_Vehicle",
                column: "VehicleCode",
                unique: true);
        }
    }
}
