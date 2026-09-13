using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveServiceFeesShareFromMerchantPaymentDetailsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceFeeShare",
                table: "VO_MerchantOrderPaymentDetail");

            migrationBuilder.AddColumn<bool>(
                name: "CompanyServiceFeeAccrued",
                table: "VO_Order",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyServiceFeeAccrued",
                table: "VO_Order");

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceFeeShare",
                table: "VO_MerchantOrderPaymentDetail",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
