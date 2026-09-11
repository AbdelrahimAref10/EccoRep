using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantIdToVehicleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MerchantId",
                table: "VO_Vehicle",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_VO_Vehicle_MerchantId",
                table: "VO_Vehicle",
                column: "MerchantId");

            migrationBuilder.AddForeignKey(
                name: "FK_VO_Vehicle_VO_Merchant_MerchantId",
                table: "VO_Vehicle",
                column: "MerchantId",
                principalTable: "VO_Merchant",
                principalColumn: "MerchantId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VO_Vehicle_VO_Merchant_MerchantId",
                table: "VO_Vehicle");

            migrationBuilder.DropIndex(
                name: "IX_VO_Vehicle_MerchantId",
                table: "VO_Vehicle");

            migrationBuilder.DropColumn(
                name: "MerchantId",
                table: "VO_Vehicle");
        }
    }
}
