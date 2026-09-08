using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerNotificationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VO_AdminNotification",
                columns: table => new
                {
                    AdminNotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    NotificationType = table.Column<int>(type: "int", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReadByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_AdminNotification", x => x.AdminNotificationId);
                    table.ForeignKey(
                        name: "FK_VO_AdminNotification_VO_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "VO_Order",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VO_AdminNotification_VO_User_ReadByUserId",
                        column: x => x.ReadByUserId,
                        principalTable: "VO_User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VO_AdminNotification_CreatedDate",
                table: "VO_AdminNotification",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_VO_AdminNotification_IsRead",
                table: "VO_AdminNotification",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_VO_AdminNotification_NotificationType",
                table: "VO_AdminNotification",
                column: "NotificationType");

            migrationBuilder.CreateIndex(
                name: "IX_VO_AdminNotification_OrderId",
                table: "VO_AdminNotification",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_AdminNotification_ReadByUserId",
                table: "VO_AdminNotification",
                column: "ReadByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VO_AdminNotification");
        }
    }
}
