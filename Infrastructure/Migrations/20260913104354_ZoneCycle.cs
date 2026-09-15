using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ZoneCycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryFees",
                table: "VO_City");

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryFee",
                table: "VO_OrderVehicle",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DestinationZoneId",
                table: "VO_Order",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ZoneId",
                table: "VO_Merchant",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ZoneId",
                table: "VO_Delivery",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ZoneId",
                table: "VO_Customer",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ZoneGroupId",
                table: "VO_City",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VO_ZoneGroup",
                columns: table => new
                {
                    ZoneGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_ZoneGroup", x => x.ZoneGroupId);
                });

            migrationBuilder.CreateTable(
                name: "VO_Zone",
                columns: table => new
                {
                    ZoneId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ZoneGroupId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_Zone", x => x.ZoneId);
                    table.ForeignKey(
                        name: "FK_VO_Zone_VO_ZoneGroup_ZoneGroupId",
                        column: x => x.ZoneGroupId,
                        principalTable: "VO_ZoneGroup",
                        principalColumn: "ZoneGroupId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VO_ZoneDeliveryRate",
                columns: table => new
                {
                    ZoneDeliveryRateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ZoneGroupId = table.Column<int>(type: "int", nullable: false),
                    FromZoneId = table.Column<int>(type: "int", nullable: false),
                    ToZoneId = table.Column<int>(type: "int", nullable: false),
                    Fee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_ZoneDeliveryRate", x => x.ZoneDeliveryRateId);
                    table.ForeignKey(
                        name: "FK_VO_ZoneDeliveryRate_VO_ZoneGroup_ZoneGroupId",
                        column: x => x.ZoneGroupId,
                        principalTable: "VO_ZoneGroup",
                        principalColumn: "ZoneGroupId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VO_ZoneDeliveryRate_VO_Zone_FromZoneId",
                        column: x => x.FromZoneId,
                        principalTable: "VO_Zone",
                        principalColumn: "ZoneId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VO_ZoneDeliveryRate_VO_Zone_ToZoneId",
                        column: x => x.ToZoneId,
                        principalTable: "VO_Zone",
                        principalColumn: "ZoneId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VO_Order_DestinationZoneId",
                table: "VO_Order",
                column: "DestinationZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_Merchant_ZoneId",
                table: "VO_Merchant",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_Delivery_ZoneId",
                table: "VO_Delivery",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_Customer_ZoneId",
                table: "VO_Customer",
                column: "ZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_City_ZoneGroupId",
                table: "VO_City",
                column: "ZoneGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_Zone_Group_Name",
                table: "VO_Zone",
                columns: new[] { "ZoneGroupId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VO_ZoneDeliveryRate_From_To",
                table: "VO_ZoneDeliveryRate",
                columns: new[] { "FromZoneId", "ToZoneId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VO_ZoneDeliveryRate_ToZoneId",
                table: "VO_ZoneDeliveryRate",
                column: "ToZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_ZoneDeliveryRate_ZoneGroupId",
                table: "VO_ZoneDeliveryRate",
                column: "ZoneGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_ZoneGroup_Name",
                table: "VO_ZoneGroup",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VO_City_VO_ZoneGroup_ZoneGroupId",
                table: "VO_City",
                column: "ZoneGroupId",
                principalTable: "VO_ZoneGroup",
                principalColumn: "ZoneGroupId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VO_Customer_VO_Zone_ZoneId",
                table: "VO_Customer",
                column: "ZoneId",
                principalTable: "VO_Zone",
                principalColumn: "ZoneId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VO_Delivery_VO_Zone_ZoneId",
                table: "VO_Delivery",
                column: "ZoneId",
                principalTable: "VO_Zone",
                principalColumn: "ZoneId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VO_Merchant_VO_Zone_ZoneId",
                table: "VO_Merchant",
                column: "ZoneId",
                principalTable: "VO_Zone",
                principalColumn: "ZoneId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VO_Order_VO_Zone_DestinationZoneId",
                table: "VO_Order",
                column: "DestinationZoneId",
                principalTable: "VO_Zone",
                principalColumn: "ZoneId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VO_City_VO_ZoneGroup_ZoneGroupId",
                table: "VO_City");

            migrationBuilder.DropForeignKey(
                name: "FK_VO_Customer_VO_Zone_ZoneId",
                table: "VO_Customer");

            migrationBuilder.DropForeignKey(
                name: "FK_VO_Delivery_VO_Zone_ZoneId",
                table: "VO_Delivery");

            migrationBuilder.DropForeignKey(
                name: "FK_VO_Merchant_VO_Zone_ZoneId",
                table: "VO_Merchant");

            migrationBuilder.DropForeignKey(
                name: "FK_VO_Order_VO_Zone_DestinationZoneId",
                table: "VO_Order");

            migrationBuilder.DropTable(
                name: "VO_ZoneDeliveryRate");

            migrationBuilder.DropTable(
                name: "VO_Zone");

            migrationBuilder.DropTable(
                name: "VO_ZoneGroup");

            migrationBuilder.DropIndex(
                name: "IX_VO_Order_DestinationZoneId",
                table: "VO_Order");

            migrationBuilder.DropIndex(
                name: "IX_VO_Merchant_ZoneId",
                table: "VO_Merchant");

            migrationBuilder.DropIndex(
                name: "IX_VO_Delivery_ZoneId",
                table: "VO_Delivery");

            migrationBuilder.DropIndex(
                name: "IX_VO_Customer_ZoneId",
                table: "VO_Customer");

            migrationBuilder.DropIndex(
                name: "IX_VO_City_ZoneGroupId",
                table: "VO_City");

            migrationBuilder.DropColumn(
                name: "DeliveryFee",
                table: "VO_OrderVehicle");

            migrationBuilder.DropColumn(
                name: "DestinationZoneId",
                table: "VO_Order");

            migrationBuilder.DropColumn(
                name: "ZoneId",
                table: "VO_Merchant");

            migrationBuilder.DropColumn(
                name: "ZoneId",
                table: "VO_Delivery");

            migrationBuilder.DropColumn(
                name: "ZoneId",
                table: "VO_Customer");

            migrationBuilder.DropColumn(
                name: "ZoneGroupId",
                table: "VO_City");

            migrationBuilder.AddColumn<decimal>(
                name: "DeliveryFees",
                table: "VO_City",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
