using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeCityTableAndAddStateToCustomerTableInsteadOfIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customer_MobileNumber_IsActive",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Customer");

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Customer",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FullAddress",
                table: "Customer",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Customer",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "Customer",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    CityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.CityId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customer_CityId",
                table: "Customer",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_MobileNumber_State",
                table: "Customer",
                columns: new[] { "MobileNumber", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_Cities_IsActive",
                table: "Cities",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_Name",
                table: "Cities",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Customer_Cities_CityId",
                table: "Customer",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "CityId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customer_Cities_CityId",
                table: "Customer");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropIndex(
                name: "IX_Customer_CityId",
                table: "Customer");

            migrationBuilder.DropIndex(
                name: "IX_Customer_MobileNumber_State",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "FullAddress",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Customer");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Customer");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Customer",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Customer_MobileNumber_IsActive",
                table: "Customer",
                columns: new[] { "MobileNumber", "IsActive" });
        }
    }
}
