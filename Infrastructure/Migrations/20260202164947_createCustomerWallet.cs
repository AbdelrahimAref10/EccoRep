using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class createCustomerWallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VO_OrderCancellationFee");

            migrationBuilder.CreateTable(
                name: "VO_CustomerWallet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    Withdraw = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Deposit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_CustomerWallet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VO_CustomerWallet_VO_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "VO_Customer",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VO_CustomerWallet_CustomerId",
                table: "VO_CustomerWallet",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_CustomerWallet_OrderId",
                table: "VO_CustomerWallet",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_CustomerWallet_State",
                table: "VO_CustomerWallet",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_VO_CustomerWallet_Type",
                table: "VO_CustomerWallet",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VO_CustomerWallet");

            migrationBuilder.CreateTable(
                name: "VO_OrderCancellationFee",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
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
        }
    }
}
