# Identity Tables Explanation

This document explains what Identity tables are created, what I configured, and what's missing.

## 📋 What Identity Creates Automatically

When you inherit from `IdentityDbContext<ApplicationUser, ApplicationRole, int>`, ASP.NET Core Identity **automatically creates** these tables:

### ✅ Standard Identity Tables (Created by IdentityDbContext)

1. **Users** (ApplicationUser)
   - Primary Key: `Id` (int)
   - Contains: UserName, Email, PasswordHash, etc.
   - **Status**: ✅ Configured with mapping file

2. **Roles** (ApplicationRole)
   - Primary Key: `Id` (int)
   - Contains: Name, NormalizedName, ConcurrencyStamp
   - **Status**: ✅ Configured with mapping file

3. **UserRoles** (IdentityUserRole<int>)
   - Primary Key: Composite (UserId, RoleId)
   - **Purpose**: Many-to-Many relationship between Users and Roles
   - **Status**: ✅ Configured with mapping file
   - **Relationship**: 
     - UserRoles.UserId → Users.Id
     - UserRoles.RoleId → Roles.Id

4. **UserClaims** (IdentityUserClaim<int>)
   - Primary Key: `Id` (int)
   - Contains: UserId, ClaimType, ClaimValue
   - **Purpose**: Store custom claims for users
   - **Status**: ✅ Configured with mapping file
   - **Relationship**: UserClaims.UserId → Users.Id

5. **RoleClaims** (IdentityRoleClaim<int>)
   - Primary Key: `Id` (int)
   - Contains: RoleId, ClaimType, ClaimValue
   - **Purpose**: Store claims for roles
   - **Status**: ✅ Configured with mapping file
   - **Relationship**: RoleClaims.RoleId → Roles.Id

6. **UserLogins** (IdentityUserLogin<int>)
   - Primary Key: Composite (LoginProvider, ProviderKey)
   - Contains: UserId, LoginProvider, ProviderKey, ProviderDisplayName
   - **Purpose**: External login providers (Google, Facebook, etc.)
   - **Status**: ✅ Configured with mapping file
   - **Relationship**: UserLogins.UserId → Users.Id

7. **UserTokens** (IdentityUserToken<int>)
   - Primary Key: Composite (UserId, LoginProvider, Name)
   - Contains: UserId, LoginProvider, Name, Value
   - **Purpose**: Store authentication tokens
   - **Status**: ✅ Configured with mapping file
   - **Relationship**: UserTokens.UserId → Users.Id

## 🔗 Current Relationships Diagram

```
Users (ApplicationUser)
  ├── UserRoles (Many-to-Many with Roles)
  │   └── RoleId → Roles.Id
  ├── UserClaims (One-to-Many)
  ├── UserLogins (One-to-Many)
  └── UserTokens (One-to-Many)

Roles (ApplicationRole)
  ├── UserRoles (Many-to-Many with Users)
  │   └── UserId → Users.Id
  └── RoleClaims (One-to-Many)
```

## ❌ What's MISSING (Custom Tables)

You mentioned you need:
1. **Permissions** table - ❌ NOT CREATED (This is a custom table, not part of Identity)
2. **RolePermissions** table - ❌ NOT CREATED (This is a custom table, not part of Identity)

### Why These Are Missing

**Permissions** and **RolePermissions** are **NOT part of ASP.NET Core Identity**. These are **custom tables** that you need to create yourself for a permission-based authorization system.

Identity provides:
- ✅ Users
- ✅ Roles
- ✅ UserRoles (User ↔ Role relationship)
- ✅ Claims (for both Users and Roles)

But Identity does **NOT** provide:
- ❌ Permissions table
- ❌ RolePermissions table (Role ↔ Permission relationship)

## 📝 What I Actually Created

### Mapping Configuration Files Created:

1. ✅ `ApplicationUserConfiguration.cs` - Maps Users table
2. ✅ `ApplicationRoleConfiguration.cs` - Maps Roles table
3. ✅ `IdentityUserRoleConfiguration.cs` - Maps UserRoles table
4. ✅ `IdentityUserClaimConfiguration.cs` - Maps UserClaims table
5. ✅ `IdentityRoleClaimConfiguration.cs` - Maps RoleClaims table
6. ✅ `IdentityUserLoginConfiguration.cs` - Maps UserLogins table
7. ✅ `IdentityUserTokenConfiguration.cs` - Maps UserTokens table

### What These Configurations Do:

- Set table names (e.g., "Users", "Roles", "UserRoles")
- Configure column names
- Set data types and constraints (max length, required, etc.)
- Configure indexes
- Configure audit properties (CreatedBy, CreatedDate, etc.)
- **BUT**: They do NOT create foreign key relationships explicitly (Identity handles this automatically)

## 🔧 How Identity Handles Relationships

When you call `base.OnModelCreating(modelBuilder)` in your `DatabaseContext`, Identity **automatically** creates:

1. **Foreign Keys**:
   - UserRoles.UserId → Users.Id
   - UserRoles.RoleId → Roles.Id
   - UserClaims.UserId → Users.Id
   - RoleClaims.RoleId → Roles.Id
   - UserLogins.UserId → Users.Id
   - UserTokens.UserId → Users.Id

2. **Indexes**:
   - Identity creates indexes automatically for performance

## 🎯 What You Need to Add

To implement **Permissions** and **RolePermissions**, you need to:

### 1. Create Permission Entity (Domain Layer)
```csharp
public class Permission : IAuditable
{
    public int PermissionId { get; private set; }
    public string PermissionName { get; private set; }
    public string Description { get; private set; }
    // Audit properties
}
```

### 2. Create RolePermission Entity (Domain Layer)
```csharp
public class RolePermission : IAuditable
{
    public int RoleId { get; private set; }
    public int PermissionId { get; private set; }
    // Navigation properties
    public ApplicationRole Role { get; private set; }
    public Permission Permission { get; private set; }
    // Audit properties
}
```

### 3. Create Mapping Configurations (Infrastructure Layer)
- `PermissionConfiguration.cs`
- `RolePermissionConfiguration.cs`

### 4. Add DbSets to DatabaseContext
```csharp
public DbSet<Permission> Permissions { get; set; }
public DbSet<RolePermission> RolePermissions { get; set; }
```

### 5. Configure Relationships
- RolePermissions.RoleId → Roles.Id
- RolePermissions.PermissionId → Permissions.PermissionId

## 📊 Complete Table Structure (After Adding Permissions)

```
Users (ApplicationUser)
  ├── UserRoles → Roles (Many-to-Many)
  ├── UserClaims
  ├── UserLogins
  └── UserTokens

Roles (ApplicationRole)
  ├── UserRoles → Users (Many-to-Many)
  ├── RoleClaims
  └── RolePermissions → Permissions (Many-to-Many) [TO BE CREATED]

Permissions (Custom) [TO BE CREATED]
  └── RolePermissions → Roles (Many-to-Many) [TO BE CREATED]

RolePermissions (Custom) [TO BE CREATED]
  ├── RoleId → Roles.Id
  └── PermissionId → Permissions.PermissionId
```

## 🚀 Next Steps

1. **Review this explanation** - Understand what's already there
2. **Decide on Permission structure** - What permissions do you need?
3. **Create Permission entities** - In Domain layer
4. **Create mapping configurations** - In Infrastructure layer
5. **Add relationships** - Configure foreign keys
6. **Create migration** - Apply to database

## ⚠️ Important Notes

1. **UserRoles table EXISTS** - It's the `IdentityUserRole<int>` table, mapped to "UserRoles"
2. **Permissions table does NOT exist** - This is custom and needs to be created
3. **RolePermissions table does NOT exist** - This is custom and needs to be created
4. **All Identity relationships are handled automatically** - You don't need to configure them manually
5. **Custom tables need manual configuration** - Permissions and RolePermissions need full configuration

