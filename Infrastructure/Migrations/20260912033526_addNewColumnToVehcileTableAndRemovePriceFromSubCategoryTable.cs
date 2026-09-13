using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addNewColumnToVehcileTableAndRemovePriceFromSubCategoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "VO_SubCategory");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "VO_Vehicle",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EngineCapacityCc",
                table: "VO_Vehicle",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "VO_Vehicle",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "VO_Vehicle",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SpeedKmh",
                table: "VO_Vehicle",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "VO_Vehicle",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "VO_Vehicle");

            migrationBuilder.DropColumn(
                name: "EngineCapacityCc",
                table: "VO_Vehicle");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "VO_Vehicle");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "VO_Vehicle");

            migrationBuilder.DropColumn(
                name: "SpeedKmh",
                table: "VO_Vehicle");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "VO_Vehicle");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "VO_SubCategory",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
