using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCityIdToMerchantAndDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "VO_Merchant",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "VO_Delivery",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Merchant_CityId",
                table: "VO_Merchant",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_CityId",
                table: "VO_Delivery",
                column: "CityId");

            migrationBuilder.AddForeignKey(
                name: "FK_VO_Delivery_VO_City_CityId",
                table: "VO_Delivery",
                column: "CityId",
                principalTable: "VO_City",
                principalColumn: "CityId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VO_Merchant_VO_City_CityId",
                table: "VO_Merchant",
                column: "CityId",
                principalTable: "VO_City",
                principalColumn: "CityId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VO_Delivery_VO_City_CityId",
                table: "VO_Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_VO_Merchant_VO_City_CityId",
                table: "VO_Merchant");

            migrationBuilder.DropIndex(
                name: "IX_Merchant_CityId",
                table: "VO_Merchant");

            migrationBuilder.DropIndex(
                name: "IX_Delivery_CityId",
                table: "VO_Delivery");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "VO_Merchant");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "VO_Delivery");
        }
    }
}
