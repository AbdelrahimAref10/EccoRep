using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCustomerTableByAddCommercialImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customer_MobileNumber_State",
                table: "VO_Customer");

            migrationBuilder.DropColumn(
                name: "FullAddress",
                table: "VO_Customer");

            migrationBuilder.AddColumn<string>(
                name: "CommercialRegisterImage",
                table: "VO_Customer",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "VO_Customer",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customer_MobileNumber",
                table: "VO_Customer",
                column: "MobileNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Customer_MobileNumber",
                table: "VO_Customer");

            migrationBuilder.DropColumn(
                name: "CommercialRegisterImage",
                table: "VO_Customer");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "VO_Customer");

            migrationBuilder.AddColumn<string>(
                name: "FullAddress",
                table: "VO_Customer",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customer_MobileNumber_State",
                table: "VO_Customer",
                columns: new[] { "MobileNumber", "State" });
        }
    }
}
