using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCustomerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customer_MobileNumber",
                table: "VO_Customer");

            migrationBuilder.DropIndex(
                name: "IX_Customer_NationalNumber",
                table: "VO_Customer");

            migrationBuilder.DropColumn(
                name: "NationalNumber",
                table: "VO_Customer");

            migrationBuilder.AddColumn<string>(
                name: "PersonalImage",
                table: "VO_Customer",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegisterAs",
                table: "VO_Customer",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "VerificationBy",
                table: "VO_Customer",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonalImage",
                table: "VO_Customer");

            migrationBuilder.DropColumn(
                name: "RegisterAs",
                table: "VO_Customer");

            migrationBuilder.DropColumn(
                name: "VerificationBy",
                table: "VO_Customer");

            migrationBuilder.AddColumn<string>(
                name: "NationalNumber",
                table: "VO_Customer",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_MobileNumber",
                table: "VO_Customer",
                column: "MobileNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customer_NationalNumber",
                table: "VO_Customer",
                column: "NationalNumber",
                unique: true);
        }
    }
}
