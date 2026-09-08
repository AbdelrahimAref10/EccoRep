using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIdentityToBeInUsersTablenly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VO_CustomerRefreshToken");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "VO_Customer");

            migrationBuilder.DropColumn(
                name: "PasswordResetCode",
                table: "VO_Customer");

            migrationBuilder.DropColumn(
                name: "PasswordResetCodeExpiry",
                table: "VO_Customer");

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetCode",
                table: "VO_User",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetCodeExpiry",
                table: "VO_User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "VO_Customer",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "VO_Delivery",
                columns: table => new
                {
                    DeliveryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MobileNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PersonalImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvitationCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    InvitationCodeExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsInvitationCodeUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_Delivery", x => x.DeliveryId);
                    table.ForeignKey(
                        name: "FK_VO_Delivery_VO_User_UserId",
                        column: x => x.UserId,
                        principalTable: "VO_User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VO_Employee",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_Employee", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_VO_Employee_VO_User_UserId",
                        column: x => x.UserId,
                        principalTable: "VO_User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VO_Merchant",
                columns: table => new
                {
                    MerchantId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MobileNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PersonalImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvitationCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    InvitationCodeExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsInvitationCodeUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_Merchant", x => x.MerchantId);
                    table.ForeignKey(
                        name: "FK_VO_Merchant_VO_User_UserId",
                        column: x => x.UserId,
                        principalTable: "VO_User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Customer_UserId",
                table: "VO_Customer",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_MobileNumber",
                table: "VO_Delivery",
                column: "MobileNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Delivery_UserId",
                table: "VO_Delivery",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employee_UserId",
                table: "VO_Employee",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Merchant_MobileNumber",
                table: "VO_Merchant",
                column: "MobileNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Merchant_UserId",
                table: "VO_Merchant",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VO_Customer_VO_User_UserId",
                table: "VO_Customer",
                column: "UserId",
                principalTable: "VO_User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VO_Customer_VO_User_UserId",
                table: "VO_Customer");

            migrationBuilder.DropTable(
                name: "VO_Delivery");

            migrationBuilder.DropTable(
                name: "VO_Employee");

            migrationBuilder.DropTable(
                name: "VO_Merchant");

            migrationBuilder.DropIndex(
                name: "IX_Customer_UserId",
                table: "VO_Customer");

            migrationBuilder.DropColumn(
                name: "PasswordResetCode",
                table: "VO_User");

            migrationBuilder.DropColumn(
                name: "PasswordResetCodeExpiry",
                table: "VO_User");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "VO_Customer");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "VO_Customer",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetCode",
                table: "VO_Customer",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetCodeExpiry",
                table: "VO_Customer",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VO_CustomerRefreshToken",
                columns: table => new
                {
                    CustomerRefreshTokenId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
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
    }
}
