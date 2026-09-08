using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTotalsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderTotals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServiceFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DeliveryFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UrgentFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAfterAllFees = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTotals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderTotals_VO_Order_OrderId",
                        column: x => x.OrderId,
                        principalTable: "VO_Order",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderTotals_OrderId",
                table: "OrderTotals",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderTotals");
        }
    }
}
