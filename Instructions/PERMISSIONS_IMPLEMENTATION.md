# Permissions and RolePermissions Implementation

This document describes the Permissions and RolePermissions implementation for the ECommerce application.

## Overview

The permission system extends ASP.NET Core Identity with a custom permission-based authorization model. This allows for fine-grained access control beyond just roles.

## Entities Created

### 1. Permission Entity

**Location**: `Domain/Models/Permission.cs`

**Properties**:
- `PermissionId` (int) - Primary Key
- `PermissionName` (string) - Unique permission name (e.g., "CreateProduct", "DeleteOrder")
- `Description` (string) - Description of what the permission allows
- `Module` (string) - Module/area this permission belongs to (e.g., "Products", "Orders", "Users")
- `IsActive` (bool) - Whether the permission is currently active
- `RolePermissions` (ICollection<RolePermission>) - Navigation property
- Audit properties: CreatedBy, CreatedDate, LastModifiedBy, LastModifiedDate

**Domain Methods**:
- `Create()` - Factory method for creating new permissions
- `Update()` - Update permission details
- `Deactivate()` - Deactivate a permission
- `Activate()` - Activate a permission

**Business Rules**:
- PermissionName cannot be empty
- Module cannot be empty
- PermissionName must be unique

### 2. RolePermission Entity

**Location**: `Domain/Models/RolePermission.cs`

**Properties**:
- `RoleId` (int) - Foreign Key to Roles.Id
- `PermissionId` (int) - Foreign Key to Permissions.PermissionId
- `Role` (ApplicationRole) - Navigation property
- `Permission` (Permission) - Navigation property
- Audit properties: CreatedBy, CreatedDate, LastModifiedBy, LastModifiedDate

**Composite Primary Key**: (RoleId, PermissionId)

**Domain Methods**:
- `Create()` - Factory method for creating role-permission relationships

**Business Rules**:
- RoleId must be greater than zero
- PermissionId must be greater than zero
- Each role-permission pair must be unique

## Database Tables

### Permissions Table

**Table Name**: `Permissions`

**Columns**:
- `PermissionId` (int, PK, Identity)
- `PermissionName` (nvarchar(256), Required, Unique)
- `Description` (nvarchar(500), Nullable)
- `Module` (nvarchar(100), Required)
- `IsActive` (bit, Default: true)
- `CreatedBy` (nvarchar(256), Nullable)
- `CreatedDate` (datetime2, Required)
- `LastModifiedBy` (nvarchar(256), Nullable)
- `LastModifiedDate` (datetime2, Required)

**Indexes**:
- `IX_Permissions_PermissionName` (Unique)
- `IX_Permissions_Module`
- `IX_Permissions_Module_IsActive` (Composite)

### RolePermissions Table

**Table Name**: `RolePermissions`

**Columns**:
- `RoleId` (int, PK, FK → Roles.Id)
- `PermissionId` (int, PK, FK → Permissions.PermissionId)
- `CreatedBy` (nvarchar(256), Nullable)
- `CreatedDate` (datetime2, Required)
- `LastModifiedBy` (nvarchar(256), Nullable)
- `LastModifiedDate` (datetime2, Required)

**Composite Primary Key**: (RoleId, PermissionId)

**Foreign Keys**:
- `RoleId` → `Roles.Id` (Cascade Delete)
- `PermissionId` → `Permissions.PermissionId` (Cascade Delete)

**Indexes**:
- `IX_RolePermissions_RoleId`
- `IX_RolePermissions_PermissionId`
- `IX_RolePermissions_RoleId_PermissionId` (Unique, Composite)

## Relationships

```
Roles (ApplicationRole)
  ├── UserRoles → Users (Many-to-Many) [Identity]
  ├── RoleClaims [Identity]
  └── RolePermissions → Permissions (Many-to-Many) [Custom]

Permissions (Permission)
  └── RolePermissions → Roles (Many-to-Many) [Custom]

RolePermissions (RolePermission)
  ├── RoleId → Roles.Id (Many-to-One)
  └── PermissionId → Permissions.PermissionId (Many-to-One)
```

## Mapping Configurations

### PermissionConfiguration

**Location**: `Infrastructure/MappingConfiguration/PermissionConfiguration.cs`

**Configuration**:
- Table name: "Permissions"
- Primary key: PermissionId
- Unique constraint on PermissionName
- Indexes for Module and Module+IsActive
- One-to-many relationship with RolePermissions

### RolePermissionConfiguration

**Location**: `Infrastructure/MappingConfiguration/RolePermissionConfiguration.cs`

**Configuration**:
- Table name: "RolePermissions"
- Composite primary key: (RoleId, PermissionId)
- Foreign keys to Roles and Permissions
- Cascade delete on both relationships
- Unique constraint on (RoleId, PermissionId)
- Indexes on RoleId and PermissionId

## Usage Example

### Creating a Permission

```csharp
var permission = Permission.Create(
    permissionName: "CreateProduct",
    description: "Allows creating new products",
    module: "Products",
    createdBy: "System"
);

await _context.Permissions.AddAsync(permission);
await _context.SaveChangesAsync();
```

### Assigning Permission to Role

```csharp
var rolePermission = RolePermission.Create(
    roleId: adminRoleId,
    permissionId: createProductPermissionId,
    createdBy: currentUserId
);

await _context.RolePermissions.AddAsync(rolePermission);
await _context.SaveChangesAsync();
```

### Checking User Permissions

```csharp
// Get all permissions for a user through their roles
var userRoles = await _userManager.GetRolesAsync(user);
var roleIds = await _context.Roles
    .Where(r => userRoles.Contains(r.Name))
    .Select(r => r.Id)
    .ToListAsync();

var userPermissions = await _context.RolePermissions
    .Where(rp => roleIds.Contains(rp.RoleId))
    .Include(rp => rp.Permission)
    .Where(rp => rp.Permission.IsActive)
    .Select(rp => rp.Permission.PermissionName)
    .Distinct()
    .ToListAsync();
```

## Architecture Compliance

✅ **Domain-Driven Design**:
- Entities with private setters
- Factory methods for creation
- Domain methods for business logic
- Encapsulation of business rules

✅ **Clean Architecture**:
- Entities in Domain layer
- Mapping configurations in Infrastructure layer
- No dependencies on external frameworks in Domain

✅ **Audit Trail**:
- All entities implement IAuditable
- Automatic audit tracking in SaveChangesAsyncWithResult

✅ **Mapping Pattern**:
- Separate configuration files
- Detailed property configurations
- Proper relationships and indexes

## Next Steps

1. **Create Migration**: Generate migration for Permissions and RolePermissions tables
2. **Seed Data**: Create initial permissions (e.g., CreateProduct, UpdateProduct, DeleteProduct)
3. **Create Commands/Queries**: Implement CQRS handlers for permission management
4. **Create Controller**: Add API endpoints for permission management
5. **Authorization Policy**: Create authorization policies based on permissions
6. **Testing**: Write unit and integration tests

## Migration Command

```bash
dotnet ef migrations add AddPermissionsAndRolePermissions --project Infrastructure --startup-project ECommerce.Server
dotnet ef database update --project Infrastructure --startup-project ECommerce.Server
```

