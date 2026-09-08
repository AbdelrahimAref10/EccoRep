using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrderCycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VehicleCode",
                table: "VO_Vehicle",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CancellationFees",
                table: "VO_City",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryFees",
                table: "VO_City",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceFees",
                table: "VO_City",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UrgentDelivery",
                table: "VO_City",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VO_CompanyTreasury",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TotalRevenue = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    TotalCancellationFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_CompanyTreasury", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VO_Order",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    SubCategoryId = table.Column<int>(type: "int", nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    ReservationDateFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReservationDateTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VehiclesCount = table.Column<int>(type: "int", nullable: false),
                    OrderSubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PassportImage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HotelName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    HotelAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HotelPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsUrgent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PaymentMethodId = table.Column<int>(type: "int", nullable: false),
                    OrderState = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_Order", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_VO_Order_VO_City_CityId",
                        column: x => x.CityId,
                        principalTable: "VO_City",
                        principalColumn: "CityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VO_Order_VO_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "VO_Customer",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VO_Order_VO_SubCategory_SubCategoryId",
                        column: x => x.SubCategoryId,
                        principalTable: "VO_SubCategory",
                        principalColumn: "SubCategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VO_OrderCancellationFee",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_OrderCancellationFee", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VO_OrderCancellationFee_VO_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "VO_Customer",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VO_OrderCancellationFee_VO_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "VO_Order",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VO_OrderPayment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    PaymentMethodId = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_OrderPayment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VO_OrderPayment_VO_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "VO_Order",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VO_OrderVehicle",
                columns: table => new
                {
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_OrderVehicle", x => new { x.OrderId, x.VehicleId });
                    table.ForeignKey(
                        name: "FK_VO_OrderVehicle_VO_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "VO_Order",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VO_OrderVehicle_VO_Vehicle_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "VO_Vehicle",
                        principalColumn: "VehicleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VO_RefundablePaypalAmount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    OrderTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CancellationFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RefundableAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_RefundablePaypalAmount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VO_RefundablePaypalAmount_VO_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "VO_Customer",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VO_RefundablePaypalAmount_VO_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "VO_Order",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VO_ReservedVehiclesPerDays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    SubCategoryId = table.Column<int>(type: "int", nullable: false),
                    VehicleCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    DateFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_ReservedVehiclesPerDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VO_ReservedVehiclesPerDays_VO_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "VO_Order",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VO_ReservedVehiclesPerDays_VO_SubCategory_SubCategoryId",
                        column: x => x.SubCategoryId,
                        principalTable: "VO_SubCategory",
                        principalColumn: "SubCategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VO_ReservedVehiclesPerDays_VO_Vehicle_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "VO_Vehicle",
                        principalColumn: "VehicleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VO_Vehicle_VehicleCode",
                table: "VO_Vehicle",
                column: "VehicleCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VO_Order_CityId",
                table: "VO_Order",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_Order_CreatedDate",
                table: "VO_Order",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_VO_Order_CustomerId",
                table: "VO_Order",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_Order_OrderCode",
                table: "VO_Order",
                column: "OrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VO_Order_OrderState",
                table: "VO_Order",
                column: "OrderState");

            migrationBuilder.CreateIndex(
                name: "IX_VO_Order_SubCategoryId",
                table: "VO_Order",
                column: "SubCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_OrderCancellationFee_CustomerId",
                table: "VO_OrderCancellationFee",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_OrderCancellationFee_OrderId",
                table: "VO_OrderCancellationFee",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VO_OrderCancellationFee_State",
                table: "VO_OrderCancellationFee",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_VO_OrderPayment_OrderId",
                table: "VO_OrderPayment",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_OrderPayment_State",
                table: "VO_OrderPayment",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_VO_OrderVehicle_VehicleId",
                table: "VO_OrderVehicle",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_RefundablePaypalAmount_CustomerId",
                table: "VO_RefundablePaypalAmount",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_RefundablePaypalAmount_OrderId",
                table: "VO_RefundablePaypalAmount",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_RefundablePaypalAmount_State",
                table: "VO_RefundablePaypalAmount",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_VO_ReservedVehiclesPerDays_OrderId",
                table: "VO_ReservedVehiclesPerDays",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_ReservedVehiclesPerDays_State",
                table: "VO_ReservedVehiclesPerDays",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_VO_ReservedVehiclesPerDays_SubCategoryId",
                table: "VO_ReservedVehiclesPerDays",
                column: "SubCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_ReservedVehiclesPerDays_SubCategoryId_Dates",
                table: "VO_ReservedVehiclesPerDays",
                columns: new[] { "SubCategoryId", "DateFrom", "DateTo" });

            migrationBuilder.CreateIndex(
                name: "IX_VO_ReservedVehiclesPerDays_VehicleId",
                table: "VO_ReservedVehiclesPerDays",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VO_CompanyTreasury");

            migrationBuilder.DropTable(
                name: "VO_OrderCancellationFee");

            migrationBuilder.DropTable(
                name: "VO_OrderPayment");

            migrationBuilder.DropTable(
                name: "VO_OrderVehicle");

            migrationBuilder.DropTable(
                name: "VO_RefundablePaypalAmount");

            migrationBuilder.DropTable(
                name: "VO_ReservedVehiclesPerDays");

            migrationBuilder.DropTable(
                name: "VO_Order");

            migrationBuilder.DropIndex(
                name: "IX_VO_Vehicle_VehicleCode",
                table: "VO_Vehicle");

            migrationBuilder.DropColumn(
                name: "VehicleCode",
                table: "VO_Vehicle");

            migrationBuilder.DropColumn(
                name: "CancellationFees",
                table: "VO_City");

            migrationBuilder.DropColumn(
                name: "DeliveryFees",
                table: "VO_City");

            migrationBuilder.DropColumn(
                name: "ServiceFees",
                table: "VO_City");

            migrationBuilder.DropColumn(
                name: "UrgentDelivery",
                table: "VO_City");
        }
    }
}
