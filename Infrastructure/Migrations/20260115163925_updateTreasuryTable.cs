using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateTreasuryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "VO_CompanyTreasury");

            migrationBuilder.DropColumn(
                name: "LastModifiedDate",
                table: "VO_CompanyTreasury");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "VO_CompanyTreasury");

            migrationBuilder.RenameColumn(
                name: "TotalRevenue",
                table: "VO_CompanyTreasury",
                newName: "DebitAmount");

            migrationBuilder.RenameColumn(
                name: "TotalCancellationFees",
                table: "VO_CompanyTreasury",
                newName: "CreditAmount");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionAr",
                table: "VO_CompanyTreasury",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEng",
                table: "VO_CompanyTreasury",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionAr",
                table: "VO_CompanyTreasury");

            migrationBuilder.DropColumn(
                name: "DescriptionEng",
                table: "VO_CompanyTreasury");

            migrationBuilder.RenameColumn(
                name: "DebitAmount",
                table: "VO_CompanyTreasury",
                newName: "TotalRevenue");

            migrationBuilder.RenameColumn(
                name: "CreditAmount",
                table: "VO_CompanyTreasury",
                newName: "TotalCancellationFees");

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "VO_CompanyTreasury",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedDate",
                table: "VO_CompanyTreasury",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "VO_CompanyTreasury",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
