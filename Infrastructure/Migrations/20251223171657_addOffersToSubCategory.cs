using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addOffersToSubCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOffer",
                table: "VO_SubCategory",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_VO_SubCategory_IsOffer",
                table: "VO_SubCategory",
                column: "IsOffer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VO_SubCategory_IsOffer",
                table: "VO_SubCategory");

            migrationBuilder.DropColumn(
                name: "IsOffer",
                table: "VO_SubCategory");
        }
    }
}
