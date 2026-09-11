using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJournalsAnsEttleTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReceiptFaultParty",
                table: "VO_Order",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptRejectNote",
                table: "VO_Order",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CashOnReceive",
                table: "VO_Merchant",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "VO_DeliveryMenOrder",
                columns: table => new
                {
                    DeliveryMenOrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    DeliveryId = table.Column<int>(type: "int", nullable: false),
                    DeliveryReceivedFromMerchant = table.Column<bool>(type: "bit", nullable: false),
                    ReceivedFromMerchantAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_DeliveryMenOrder", x => x.DeliveryMenOrderId);
                    table.ForeignKey(
                        name: "FK_VO_DeliveryMenOrder_VO_Delivery_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "VO_Delivery",
                        principalColumn: "DeliveryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VO_DeliveryMenOrder_VO_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "VO_Order",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VO_DeliveryMenOrder_VO_Vehicle_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "VO_Vehicle",
                        principalColumn: "VehicleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VO_DeliveryOrderPaymentDetail",
                columns: table => new
                {
                    DeliveryOrderPaymentDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    DeliveryId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    DeliveryFeeShare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_DeliveryOrderPaymentDetail", x => x.DeliveryOrderPaymentDetailId);
                    table.ForeignKey(
                        name: "FK_VO_DeliveryOrderPaymentDetail_VO_Delivery_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "VO_Delivery",
                        principalColumn: "DeliveryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VO_DeliveryOrderPaymentDetail_VO_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "VO_Order",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VO_DeliveryOrderPaymentDetail_VO_Vehicle_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "VO_Vehicle",
                        principalColumn: "VehicleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VO_MerchantOrder",
                columns: table => new
                {
                    MerchantOrderId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    MerchantId = table.Column<int>(type: "int", nullable: false),
                    ResponseStatus = table.Column<int>(type: "int", nullable: false),
                    RejectReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_MerchantOrder", x => x.MerchantOrderId);
                    table.ForeignKey(
                        name: "FK_VO_MerchantOrder_VO_Merchant_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "VO_Merchant",
                        principalColumn: "MerchantId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VO_MerchantOrder_VO_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "VO_Order",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VO_MerchantOrderPaymentDetail",
                columns: table => new
                {
                    MerchantOrderPaymentDetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    MerchantId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    VehicleRental = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServiceFeeShare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_MerchantOrderPaymentDetail", x => x.MerchantOrderPaymentDetailId);
                    table.ForeignKey(
                        name: "FK_VO_MerchantOrderPaymentDetail_VO_Merchant_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "VO_Merchant",
                        principalColumn: "MerchantId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VO_MerchantOrderPaymentDetail_VO_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "VO_Order",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VO_MerchantOrderPaymentDetail_VO_Vehicle_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "VO_Vehicle",
                        principalColumn: "VehicleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VO_OrderJournal",
                columns: table => new
                {
                    OrderJournalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    PartyType = table.Column<int>(type: "int", nullable: false),
                    PartyId = table.Column<int>(type: "int", nullable: true),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EntryKind = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FaultParty = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_OrderJournal", x => x.OrderJournalId);
                    table.ForeignKey(
                        name: "FK_VO_OrderJournal_VO_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "VO_Order",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VO_DeliveryMenOrder_DeliveryId",
                table: "VO_DeliveryMenOrder",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_DeliveryMenOrder_Order_Vehicle",
                table: "VO_DeliveryMenOrder",
                columns: new[] { "OrderId", "VehicleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VO_DeliveryMenOrder_VehicleId",
                table: "VO_DeliveryMenOrder",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_DeliveryOrderPaymentDetail_DeliveryId",
                table: "VO_DeliveryOrderPaymentDetail",
                column: "DeliveryId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_DeliveryOrderPaymentDetail_Order_Vehicle",
                table: "VO_DeliveryOrderPaymentDetail",
                columns: new[] { "OrderId", "VehicleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VO_DeliveryOrderPaymentDetail_VehicleId",
                table: "VO_DeliveryOrderPaymentDetail",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_MerchantOrder_MerchantId",
                table: "VO_MerchantOrder",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_MerchantOrder_Order_Merchant",
                table: "VO_MerchantOrder",
                columns: new[] { "OrderId", "MerchantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VO_MerchantOrderPaymentDetail_MerchantId",
                table: "VO_MerchantOrderPaymentDetail",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_MerchantOrderPaymentDetail_Order_Vehicle",
                table: "VO_MerchantOrderPaymentDetail",
                columns: new[] { "OrderId", "VehicleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VO_MerchantOrderPaymentDetail_VehicleId",
                table: "VO_MerchantOrderPaymentDetail",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_OrderJournal_IdempotencyKey",
                table: "VO_OrderJournal",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VO_OrderJournal_Order_Party",
                table: "VO_OrderJournal",
                columns: new[] { "OrderId", "PartyType", "PartyId" });

            migrationBuilder.CreateIndex(
                name: "IX_VO_OrderJournal_Party_Created",
                table: "VO_OrderJournal",
                columns: new[] { "PartyType", "PartyId", "CreatedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VO_DeliveryMenOrder");

            migrationBuilder.DropTable(
                name: "VO_DeliveryOrderPaymentDetail");

            migrationBuilder.DropTable(
                name: "VO_MerchantOrder");

            migrationBuilder.DropTable(
                name: "VO_MerchantOrderPaymentDetail");

            migrationBuilder.DropTable(
                name: "VO_OrderJournal");

            migrationBuilder.DropColumn(
                name: "ReceiptFaultParty",
                table: "VO_Order");

            migrationBuilder.DropColumn(
                name: "ReceiptRejectNote",
                table: "VO_Order");

            migrationBuilder.DropColumn(
                name: "CashOnReceive",
                table: "VO_Merchant");
        }
    }
}
