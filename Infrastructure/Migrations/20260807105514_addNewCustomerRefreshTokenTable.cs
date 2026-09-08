using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addNewCustomerRefreshTokenTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VO_CustomerRefreshToken",
                columns: table => new
                {
                    CustomerRefreshTokenId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_CustomerRefreshToken", x => x.CustomerRefreshTokenId);
                    table.ForeignKey(
                        name: "FK_VO_CustomerRefreshToken_VO_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "VO_Customer",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VO_CustomerRefreshToken_CustomerId",
                table: "VO_CustomerRefreshToken",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_CustomerRefreshToken_CustomerId_IsRevoked_IsUsed",
                table: "VO_CustomerRefreshToken",
                columns: new[] { "CustomerId", "IsRevoked", "IsUsed" });

            migrationBuilder.CreateIndex(
                name: "IX_VO_CustomerRefreshToken_Token",
                table: "VO_CustomerRefreshToken",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VO_CustomerRefreshToken");
        }
    }
}
