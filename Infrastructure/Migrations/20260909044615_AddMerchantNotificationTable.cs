using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMerchantNotificationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VO_MerchantNotification",
                columns: table => new
                {
                    MerchantNotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MerchantId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    NotificationType = table.Column<int>(type: "int", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_MerchantNotification", x => x.MerchantNotificationId);
                    table.ForeignKey(
                        name: "FK_VO_MerchantNotification_VO_Merchant_MerchantId",
                        column: x => x.MerchantId,
                        principalTable: "VO_Merchant",
                        principalColumn: "MerchantId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VO_MerchantNotification_VO_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "VO_Order",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VO_MerchantNotification_CreatedDate",
                table: "VO_MerchantNotification",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_VO_MerchantNotification_IsRead",
                table: "VO_MerchantNotification",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_VO_MerchantNotification_MerchantId",
                table: "VO_MerchantNotification",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_MerchantNotification_OrderId",
                table: "VO_MerchantNotification",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VO_MerchantNotification");
        }
    }
}
