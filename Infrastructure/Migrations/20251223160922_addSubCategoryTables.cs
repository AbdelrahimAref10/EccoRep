using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addSubCategoryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customer_Cities_CityId",
                table: "Customer");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_User_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_RoleClaims_Role_RoleId",
                table: "RoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionId",
                table: "RolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Role_RoleId",
                table: "RolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserClaims_User_UserId",
                table: "UserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_UserLogins_User_UserId",
                table: "UserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Role_RoleId",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_User_UserId",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserTokens_User_UserId",
                table: "UserTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Categories_CategoryId",
                table: "Vehicles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Vehicles",
                table: "Vehicles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserTokens",
                table: "UserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRoles",
                table: "UserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserLogins",
                table: "UserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserClaims",
                table: "UserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_User",
                table: "User");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RolePermissions",
                table: "RolePermissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoleClaims",
                table: "RoleClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Role",
                table: "Role");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Permissions",
                table: "Permissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Customer",
                table: "Customer");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cities",
                table: "Cities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categories",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "PricePerHour",
                table: "Vehicles");

            migrationBuilder.RenameTable(
                name: "Vehicles",
                newName: "VO_Vehicle");

            migrationBuilder.RenameTable(
                name: "UserTokens",
                newName: "VO_UserToken");

            migrationBuilder.RenameTable(
                name: "UserRoles",
                newName: "VO_UserRole");

            migrationBuilder.RenameTable(
                name: "UserLogins",
                newName: "VO_UserLogin");

            migrationBuilder.RenameTable(
                name: "UserClaims",
                newName: "VO_UserClaim");

            migrationBuilder.RenameTable(
                name: "User",
                newName: "VO_User");

            migrationBuilder.RenameTable(
                name: "RolePermissions",
                newName: "VO_RolePermission");

            migrationBuilder.RenameTable(
                name: "RoleClaims",
                newName: "VO_RoleClaim");

            migrationBuilder.RenameTable(
                name: "Role",
                newName: "VO_Role");

            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                newName: "VO_RefreshToken");

            migrationBuilder.RenameTable(
                name: "Permissions",
                newName: "VO_Permission");

            migrationBuilder.RenameTable(
                name: "Customer",
                newName: "VO_Customer");

            migrationBuilder.RenameTable(
                name: "Cities",
                newName: "VO_City");

            migrationBuilder.RenameTable(
                name: "Categories",
                newName: "VO_Category");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "VO_Vehicle",
                newName: "SubCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_Vehicles_Status",
                table: "VO_Vehicle",
                newName: "IX_VO_Vehicle_Status");

            migrationBuilder.RenameIndex(
                name: "IX_Vehicles_CreatedThisMonth",
                table: "VO_Vehicle",
                newName: "IX_VO_Vehicle_CreatedThisMonth");

            migrationBuilder.RenameIndex(
                name: "IX_Vehicles_CategoryId_Status",
                table: "VO_Vehicle",
                newName: "IX_VO_Vehicle_SubCategoryId_Status");

            migrationBuilder.RenameIndex(
                name: "IX_Vehicles_CategoryId",
                table: "VO_Vehicle",
                newName: "IX_VO_Vehicle_SubCategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoles_RoleId",
                table: "VO_UserRole",
                newName: "IX_VO_UserRole_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_UserLogins_UserId",
                table: "VO_UserLogin",
                newName: "IX_VO_UserLogin_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UserClaims_UserId",
                table: "VO_UserClaim",
                newName: "IX_VO_UserClaim_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_RoleClaims_RoleId",
                table: "VO_RoleClaim",
                newName: "IX_VO_RoleClaim_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_UserId_IsRevoked_IsUsed",
                table: "VO_RefreshToken",
                newName: "IX_VO_RefreshToken_UserId_IsRevoked_IsUsed");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_UserId",
                table: "VO_RefreshToken",
                newName: "IX_VO_RefreshToken_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_Token",
                table: "VO_RefreshToken",
                newName: "IX_VO_RefreshToken_Token");

            migrationBuilder.RenameIndex(
                name: "IX_Customer_CityId",
                table: "VO_Customer",
                newName: "IX_VO_Customer_CityId");

            migrationBuilder.RenameIndex(
                name: "IX_Categories_Name",
                table: "VO_Category",
                newName: "IX_VO_Category_Name");

            migrationBuilder.RenameIndex(
                name: "IX_Categories_IsActive",
                table: "VO_Category",
                newName: "IX_VO_Category_IsActive");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VO_Vehicle",
                table: "VO_Vehicle",
                column: "VehicleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VO_UserToken",
                table: "VO_UserToken",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_VO_UserRole",
                table: "VO_UserRole",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_VO_UserLogin",
                table: "VO_UserLogin",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_VO_UserClaim",
                table: "VO_UserClaim",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VO_User",
                table: "VO_User",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VO_RolePermission",
                table: "VO_RolePermission",
                columns: new[] { "RoleId", "PermissionId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_VO_RoleClaim",
                table: "VO_RoleClaim",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VO_Role",
                table: "VO_Role",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VO_RefreshToken",
                table: "VO_RefreshToken",
                column: "RefreshTokenId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VO_Permission",
                table: "VO_Permission",
                column: "PermissionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VO_Customer",
                table: "VO_Customer",
                column: "CustomerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VO_City",
                table: "VO_City",
                column: "CityId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VO_Category",
                table: "VO_Category",
                column: "CategoryId");

            migrationBuilder.CreateTable(
                name: "VO_SubCategory",
                columns: table => new
                {
                    SubCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VO_SubCategory", x => x.SubCategoryId);
                    table.ForeignKey(
                        name: "FK_VO_SubCategory_VO_Category_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "VO_Category",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VO_SubCategory_CategoryId",
                table: "VO_SubCategory",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_VO_SubCategory_IsActive",
                table: "VO_SubCategory",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_VO_SubCategory_Name",
                table: "VO_SubCategory",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VO_Customer_VO_City_CityId",
                table: "VO_Customer",
                column: "CityId",
                principalTable: "VO_City",
                principalColumn: "CityId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VO_RefreshToken_VO_User_UserId",
                table: "VO_RefreshToken",
                column: "UserId",
                principalTable: "VO_User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VO_RoleClaim_VO_Role_RoleId",
                table: "VO_RoleClaim",
                column: "RoleId",
                principalTable: "VO_Role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VO_RolePermission_VO_Permission_PermissionId",
                table: "VO_RolePermission",
                column: "PermissionId",
                principalTable: "VO_Permission",
                principalColumn: "PermissionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VO_RolePermission_VO_Role_RoleId",
                table: "VO_RolePermission",
                column: "RoleId",
                principalTable: "VO_Role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VO_UserClaim_VO_User_UserId",
                table: "VO_UserClaim",
                column: "UserId",
                principalTable: "VO_User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VO_UserLogin_VO_User_UserId",
                table: "VO_UserLogin",
                column: "UserId",
                principalTable: "VO_User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VO_UserRole_VO_Role_RoleId",
                table: "VO_UserRole",
                column: "RoleId",
                principalTable: "VO_Role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VO_UserRole_VO_User_UserId",
                table: "VO_UserRole",
                column: "UserId",
                principalTable: "VO_User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VO_UserToken_VO_User_UserId",
                table: "VO_UserToken",
                column: "UserId",
                principalTable: "VO_User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VO_Vehicle_VO_SubCategory_SubCategoryId",
                table: "VO_Vehicle",
                column: "SubCategoryId",
                principalTable: "VO_SubCategory",
                principalColumn: "SubCategoryId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VO_Customer_VO_City_CityId",
                table: "VO_Customer");

            migrationBuilder.DropForeignKey(
                name: "FK_VO_RefreshToken_VO_User_UserId",
                table: "VO_RefreshToken");

            migrationBuilder.DropForeignKey(
                name: "FK_VO_RoleClaim_VO_Role_RoleId",
                table: "VO_RoleClaim");

            migrationBuilder.DropForeignKey(
                name: "FK_VO_RolePermission_VO_Permission_PermissionId",
                table: "VO_RolePermission");

            migrationBuilder.DropForeignKey(
                name: "FK_VO_RolePermission_VO_Role_RoleId",
                table: "VO_RolePermission");

            migrationBuilder.DropForeignKey(
                name: "FK_VO_UserClaim_VO_User_UserId",
                table: "VO_UserClaim");

            migrationBuilder.DropForeignKey(
                name: "FK_VO_UserLogin_VO_User_UserId",
                table: "VO_UserLogin");

            migrationBuilder.DropForeignKey(
                name: "FK_VO_UserRole_VO_Role_RoleId",
                table: "VO_UserRole");

            migrationBuilder.DropForeignKey(
                name: "FK_VO_UserRole_VO_User_UserId",
                table: "VO_UserRole");

            migrationBuilder.DropForeignKey(
                name: "FK_VO_UserToken_VO_User_UserId",
                table: "VO_UserToken");

            migrationBuilder.DropForeignKey(
                name: "FK_VO_Vehicle_VO_SubCategory_SubCategoryId",
                table: "VO_Vehicle");

            migrationBuilder.DropTable(
                name: "VO_SubCategory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VO_Vehicle",
                table: "VO_Vehicle");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VO_UserToken",
                table: "VO_UserToken");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VO_UserRole",
                table: "VO_UserRole");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VO_UserLogin",
                table: "VO_UserLogin");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VO_UserClaim",
                table: "VO_UserClaim");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VO_User",
                table: "VO_User");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VO_RolePermission",
                table: "VO_RolePermission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VO_RoleClaim",
                table: "VO_RoleClaim");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VO_Role",
                table: "VO_Role");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VO_RefreshToken",
                table: "VO_RefreshToken");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VO_Permission",
                table: "VO_Permission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VO_Customer",
                table: "VO_Customer");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VO_City",
                table: "VO_City");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VO_Category",
                table: "VO_Category");

            migrationBuilder.RenameTable(
                name: "VO_Vehicle",
                newName: "Vehicles");

            migrationBuilder.RenameTable(
                name: "VO_UserToken",
                newName: "UserTokens");

            migrationBuilder.RenameTable(
                name: "VO_UserRole",
                newName: "UserRoles");

            migrationBuilder.RenameTable(
                name: "VO_UserLogin",
                newName: "UserLogins");

            migrationBuilder.RenameTable(
                name: "VO_UserClaim",
                newName: "UserClaims");

            migrationBuilder.RenameTable(
                name: "VO_User",
                newName: "User");

            migrationBuilder.RenameTable(
                name: "VO_RolePermission",
                newName: "RolePermissions");

            migrationBuilder.RenameTable(
                name: "VO_RoleClaim",
                newName: "RoleClaims");

            migrationBuilder.RenameTable(
                name: "VO_Role",
                newName: "Role");

            migrationBuilder.RenameTable(
                name: "VO_RefreshToken",
                newName: "RefreshTokens");

            migrationBuilder.RenameTable(
                name: "VO_Permission",
                newName: "Permissions");

            migrationBuilder.RenameTable(
                name: "VO_Customer",
                newName: "Customer");

            migrationBuilder.RenameTable(
                name: "VO_City",
                newName: "Cities");

            migrationBuilder.RenameTable(
                name: "VO_Category",
                newName: "Categories");

            migrationBuilder.RenameColumn(
                name: "SubCategoryId",
                table: "Vehicles",
                newName: "CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_VO_Vehicle_SubCategoryId_Status",
                table: "Vehicles",
                newName: "IX_Vehicles_CategoryId_Status");

            migrationBuilder.RenameIndex(
                name: "IX_VO_Vehicle_SubCategoryId",
                table: "Vehicles",
                newName: "IX_Vehicles_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_VO_Vehicle_Status",
                table: "Vehicles",
                newName: "IX_Vehicles_Status");

            migrationBuilder.RenameIndex(
                name: "IX_VO_Vehicle_CreatedThisMonth",
                table: "Vehicles",
                newName: "IX_Vehicles_CreatedThisMonth");

            migrationBuilder.RenameIndex(
                name: "IX_VO_UserRole_RoleId",
                table: "UserRoles",
                newName: "IX_UserRoles_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_VO_UserLogin_UserId",
                table: "UserLogins",
                newName: "IX_UserLogins_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_VO_UserClaim_UserId",
                table: "UserClaims",
                newName: "IX_UserClaims_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_VO_RoleClaim_RoleId",
                table: "RoleClaims",
                newName: "IX_RoleClaims_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_VO_RefreshToken_UserId_IsRevoked_IsUsed",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_UserId_IsRevoked_IsUsed");

            migrationBuilder.RenameIndex(
                name: "IX_VO_RefreshToken_UserId",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_VO_RefreshToken_Token",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_Token");

            migrationBuilder.RenameIndex(
                name: "IX_VO_Customer_CityId",
                table: "Customer",
                newName: "IX_Customer_CityId");

            migrationBuilder.RenameIndex(
                name: "IX_VO_Category_Name",
                table: "Categories",
                newName: "IX_Categories_Name");

            migrationBuilder.RenameIndex(
                name: "IX_VO_Category_IsActive",
                table: "Categories",
                newName: "IX_Categories_IsActive");

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerHour",
                table: "Vehicles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Vehicles",
                table: "Vehicles",
                column: "VehicleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserTokens",
                table: "UserTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRoles",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserLogins",
                table: "UserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserClaims",
                table: "UserClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_User",
                table: "User",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RolePermissions",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoleClaims",
                table: "RoleClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Role",
                table: "Role",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RefreshTokens",
                table: "RefreshTokens",
                column: "RefreshTokenId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Permissions",
                table: "Permissions",
                column: "PermissionId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Customer",
                table: "Customer",
                column: "CustomerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cities",
                table: "Cities",
                column: "CityId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categories",
                table: "Categories",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customer_Cities_CityId",
                table: "Customer",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "CityId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_User_UserId",
                table: "RefreshTokens",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoleClaims_Role_RoleId",
                table: "RoleClaims",
                column: "RoleId",
                principalTable: "Role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId",
                principalTable: "Permissions",
                principalColumn: "PermissionId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Role_RoleId",
                table: "RolePermissions",
                column: "RoleId",
                principalTable: "Role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserClaims_User_UserId",
                table: "UserClaims",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserLogins_User_UserId",
                table: "UserLogins",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Role_RoleId",
                table: "UserRoles",
                column: "RoleId",
                principalTable: "Role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_User_UserId",
                table: "UserRoles",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserTokens_User_UserId",
                table: "UserTokens",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Categories_CategoryId",
                table: "Vehicles",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
