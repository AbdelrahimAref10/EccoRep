using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateCustomerTableToAddTokenLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AndriodDevice",
                table: "VO_Customer",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IosDevice",
                table: "VO_Customer",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VO_CustomerLocation",
                columns: table => new
                {
                    CustomerLocationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_CustomerLocation", x => x.CustomerLocationId);
                    table.ForeignKey(
                        name: "FK_VO_CustomerLocation_VO_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "VO_Customer",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLocation_CustomerId",
                table: "VO_CustomerLocation",
                column: "CustomerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VO_CustomerLocation");

            migrationBuilder.DropColumn(
                name: "AndriodDevice",
                table: "VO_Customer");

            migrationBuilder.DropColumn(
                name: "IosDevice",
                table: "VO_Customer");
        }
    }
}
