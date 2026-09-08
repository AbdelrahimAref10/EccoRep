using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addCityToCategoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "VO_Category",
                type: "int",    
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_VO_Category_CityId",
                table: "VO_Category",
                column: "CityId");

            migrationBuilder.AddForeignKey(
                name: "FK_VO_Category_VO_City_CityId",
                table: "VO_Category",
                column: "CityId",
                principalTable: "VO_City",
                principalColumn: "CityId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VO_Category_VO_City_CityId",
                table: "VO_Category");

            migrationBuilder.DropIndex(
                name: "IX_VO_Category_CityId",
                table: "VO_Category");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "VO_Category");
        }
    }
}
