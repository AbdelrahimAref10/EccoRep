using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderCycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DeliveredToCustomer",
                table: "VO_OrderVehicle",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredToCustomerAt",
                table: "VO_OrderVehicle",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveredToCustomerImageUrl",
                table: "VO_OrderVehicle",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DeliveredToOwner",
                table: "VO_OrderVehicle",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredToOwnerAt",
                table: "VO_OrderVehicle",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveredToOwnerImageUrl",
                table: "VO_OrderVehicle",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DeliveryFailed",
                table: "VO_OrderVehicle",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryFailureFaultParty",
                table: "VO_OrderVehicle",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryFailureReason",
                table: "VO_OrderVehicle",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReceivedFromCustomer",
                table: "VO_OrderVehicle",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedFromCustomerAt",
                table: "VO_OrderVehicle",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceivedFromCustomerImageUrl",
                table: "VO_OrderVehicle",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReceivedFromOwner",
                table: "VO_OrderVehicle",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedFromOwnerAt",
                table: "VO_OrderVehicle",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceivedFromOwnerImageUrl",
                table: "VO_OrderVehicle",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleId",
                table: "VO_OrderJournal",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OrderDeliveryFailed",
                table: "VO_Order",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OrderDeliveryFailureFaultParty",
                table: "VO_Order",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderDeliveryFailureReason",
                table: "VO_Order",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OrderTotalDebitedToCompany",
                table: "VO_Order",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_VO_OrderJournal_Order_Vehicle",
                table: "VO_OrderJournal",
                columns: new[] { "OrderId", "VehicleId" });

            migrationBuilder.CreateIndex(
                name: "IX_VO_OrderJournal_VehicleId",
                table: "VO_OrderJournal",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_VO_OrderJournal_VO_Vehicle_VehicleId",
                table: "VO_OrderJournal",
                column: "VehicleId",
                principalTable: "VO_Vehicle",
                principalColumn: "VehicleId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VO_OrderJournal_VO_Vehicle_VehicleId",
                table: "VO_OrderJournal");

            migrationBuilder.DropIndex(
                name: "IX_VO_OrderJournal_Order_Vehicle",
                table: "VO_OrderJournal");

            migrationBuilder.DropIndex(
                name: "IX_VO_OrderJournal_VehicleId",
                table: "VO_OrderJournal");

            migrationBuilder.DropColumn(
                name: "DeliveredToCustomer",
                table: "VO_OrderVehicle");

            migrationBuilder.DropColumn(
                name: "DeliveredToCustomerAt",
                table: "VO_OrderVehicle");

            migrationBuilder.DropColumn(
                name: "DeliveredToCustomerImageUrl",
                table: "VO_OrderVehicle");

            migrationBuilder.DropColumn(
                name: "DeliveredToOwner",
                table: "VO_OrderVehicle");

            migrationBuilder.DropColumn(
                name: "DeliveredToOwnerAt",
                table: "VO_OrderVehicle");

            migrationBuilder.DropColumn(
                name: "DeliveredToOwnerImageUrl",
                table: "VO_OrderVehicle");

            migrationBuilder.DropColumn(
                name: "DeliveryFailed",
                table: "VO_OrderVehicle");

            migrationBuilder.DropColumn(
                name: "DeliveryFailureFaultParty",
                table: "VO_OrderVehicle");

            migrationBuilder.DropColumn(
                name: "DeliveryFailureReason",
                table: "VO_OrderVehicle");

            migrationBuilder.DropColumn(
                name: "ReceivedFromCustomer",
                table: "VO_OrderVehicle");

            migrationBuilder.DropColumn(
                name: "ReceivedFromCustomerAt",
                table: "VO_OrderVehicle");

            migrationBuilder.DropColumn(
                name: "ReceivedFromCustomerImageUrl",
                table: "VO_OrderVehicle");

            migrationBuilder.DropColumn(
                name: "ReceivedFromOwner",
                table: "VO_OrderVehicle");

            migrationBuilder.DropColumn(
                name: "ReceivedFromOwnerAt",
                table: "VO_OrderVehicle");

            migrationBuilder.DropColumn(
                name: "ReceivedFromOwnerImageUrl",
                table: "VO_OrderVehicle");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "VO_OrderJournal");

            migrationBuilder.DropColumn(
                name: "OrderDeliveryFailed",
                table: "VO_Order");

            migrationBuilder.DropColumn(
                name: "OrderDeliveryFailureFaultParty",
                table: "VO_Order");

            migrationBuilder.DropColumn(
                name: "OrderDeliveryFailureReason",
                table: "VO_Order");

            migrationBuilder.DropColumn(
                name: "OrderTotalDebitedToCompany",
                table: "VO_Order");
        }
    }
}
