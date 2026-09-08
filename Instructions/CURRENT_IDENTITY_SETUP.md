# Current Identity Setup - Complete Overview

## ✅ What EXISTS Now (All Identity Tables)

### 1. **Users Table** (`ApplicationUser`)
- **File**: `Domain/Models/ApplicationUser.cs`
- **Mapping**: `Infrastructure/MappingConfiguration/ApplicationUserConfiguration.cs`
- **Table Name**: `Users`
- **Primary Key**: `Id` (int)
- **Columns**: 
  - Id, UserName, NormalizedUserName, Email, NormalizedEmail
  - PasswordHash, SecurityStamp, ConcurrencyStamp
  - PhoneNumber, EmailConfirmed, PhoneNumberConfirmed
  - TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount
  - **Audit**: CreatedBy, CreatedDate, LastModifiedBy, LastModifiedDate

### 2. **Roles Table** (`ApplicationRole`)
- **File**: `Domain/Models/ApplicationRole.cs`
- **Mapping**: `Infrastructure/MappingConfiguration/ApplicationRoleConfiguration.cs`
- **Table Name**: `Roles`
- **Primary Key**: `Id` (int)
- **Columns**: 
  - Id, Name, NormalizedName, ConcurrencyStamp
  - **Audit**: CreatedBy, CreatedDate, LastModifiedBy, LastModifiedDate

### 3. **UserRoles Table** (`IdentityUserRole<int>`) ✅ **THIS IS YOUR USER-ROLE RELATIONSHIP**
- **Mapping**: `Infrastructure/MappingConfiguration/IdentityUserRoleConfiguration.cs`
- **Table Name**: `UserRoles`
- **Primary Key**: Composite (UserId, RoleId)
- **Columns**: 
  - UserId (int) → Foreign Key to Users.Id
  - RoleId (int) → Foreign Key to Roles.Id
- **Purpose**: Many-to-Many relationship between Users and Roles
- **Relationship**: 
  ```
  Users ←→ UserRoles ←→ Roles
  ```

### 4. **UserClaims Table** (`IdentityUserClaim<int>`)
- **Mapping**: `Infrastructure/MappingConfiguration/IdentityUserClaimConfiguration.cs`
- **Table Name**: `UserClaims`
- **Primary Key**: `Id` (int)
- **Columns**: 
  - Id, UserId (int) → Foreign Key to Users.Id
  - ClaimType, ClaimValue
- **Purpose**: Store custom claims for individual users

### 5. **RoleClaims Table** (`IdentityRoleClaim<int>`)
- **Mapping**: `Infrastructure/MappingConfiguration/IdentityRoleClaimConfiguration.cs`
- **Table Name**: `RoleClaims`
- **Primary Key**: `Id` (int)
- **Columns**: 
  - Id, RoleId (int) → Foreign Key to Roles.Id
  - ClaimType, ClaimValue
- **Purpose**: Store claims for roles

### 6. **UserLogins Table** (`IdentityUserLogin<int>`)
- **Mapping**: `Infrastructure/MappingConfiguration/IdentityUserLoginConfiguration.cs`
- **Table Name**: `UserLogins`
- **Primary Key**: Composite (LoginProvider, ProviderKey)
- **Columns**: 
  - UserId (int) → Foreign Key to Users.Id
  - LoginProvider, ProviderKey, ProviderDisplayName
- **Purpose**: External login providers (Google, Facebook, etc.)

### 7. **UserTokens Table** (`IdentityUserToken<int>`)
- **Mapping**: `Infrastructure/MappingConfiguration/IdentityUserTokenConfiguration.cs`
- **Table Name**: `UserTokens`
- **Primary Key**: Composite (UserId, LoginProvider, Name)
- **Columns**: 
  - UserId (int) → Foreign Key to Users.Id
  - LoginProvider, Name, Value
- **Purpose**: Store authentication tokens

## ❌ What's MISSING (Custom Tables You Need)

### 1. **Permissions Table** ❌
- **Status**: NOT CREATED
- **Reason**: Not part of Identity - this is a custom table
- **What you need**: 
  - Domain entity: `Permission`
  - Mapping configuration: `PermissionConfiguration.cs`
  - DbSet in DatabaseContext

### 2. **RolePermissions Table** ❌
- **Status**: NOT CREATED
- **Reason**: Not part of Identity - this is a custom table
- **What you need**: 
  - Domain entity: `RolePermission`
  - Mapping configuration: `RolePermissionConfiguration.cs`
  - DbSet in DatabaseContext
  - Relationships:
    - RolePermissions.RoleId → Roles.Id
    - RolePermissions.PermissionId → Permissions.PermissionId

## 🔗 Current Relationships (Automatic by Identity)

Identity **automatically creates** these foreign key relationships when you call `base.OnModelCreating(modelBuilder)`:

```
Users (Id)
  ├── UserRoles.UserId → Users.Id ✅
  ├── UserClaims.UserId → Users.Id ✅
  ├── UserLogins.UserId → Users.Id ✅
  └── UserTokens.UserId → Users.Id ✅

Roles (Id)
  ├── UserRoles.RoleId → Roles.Id ✅
  └── RoleClaims.RoleId → Roles.Id ✅
```

**Note**: These relationships are created automatically by IdentityDbContext. You don't need to configure them manually in the mapping files.

## 📋 Summary

### ✅ What I Created:
1. ✅ All 7 Identity table mappings with detailed configurations
2. ✅ Table names, column names, data types, constraints
3. ✅ Indexes for performance
4. ✅ Audit properties configuration
5. ✅ **UserRoles table mapping** (this is your User-Role relationship table)

### ❌ What's Missing (You Need to Create):
1. ❌ **Permissions** entity and table
2. ❌ **RolePermissions** entity and table (Role ↔ Permission relationship)

### 🎯 Key Points:
- **UserRoles EXISTS** ✅ - This is the many-to-many table between Users and Roles
- **Permissions does NOT exist** ❌ - Custom table needed
- **RolePermissions does NOT exist** ❌ - Custom table needed
- **All Identity relationships are automatic** ✅ - No manual FK configuration needed
- **Custom tables need full configuration** ⚠️ - Permissions and RolePermissions need to be created from scratch

## 🚀 What You Should Do Next

1. **Review**: Understand that UserRoles table already exists and connects Users to Roles
2. **Decide**: What permissions do you need? (e.g., "CreateProduct", "DeleteProduct", "ViewReports")
3. **Create**: Permission entity in Domain layer
4. **Create**: RolePermission entity in Domain layer (join table)
5. **Create**: Mapping configurations for both
6. **Add**: DbSets to DatabaseContext
7. **Configure**: Relationships between RolePermissions ↔ Roles and RolePermissions ↔ Permissions
8. **Migrate**: Create and apply database migration

