# Database Cleanup Instructions

## ⚠️ IMPORTANT: Database Still Contains Old Tables

When you delete migration files from the codebase, **the database still contains**:
1. ✅ All existing tables (Product, Category, old Identity tables, etc.)
2. ✅ Migration history in `__EFMigrationsHistory` table

You need to **manually clean the database** before creating new migrations.

## 🗑️ Method 1: Using SQL Script (Recommended)

### Steps:

1. **Open SQL Server Management Studio (SSMS)**
2. **Connect to your database server**: `DESKTOP-301RNP6\SQL20222`
3. **Open the database**: `EcommerceAdmin`
4. **Open and run** the `CleanDatabase.sql` script from the project root
5. **Verify**: All tables should be removed

### What the script does:
- Disables all foreign key constraints
- Drops all user tables (Product, Category, Identity tables, etc.)
- Removes the `__EFMigrationsHistory` table
- Cleans the database completely

## 🗑️ Method 2: Using Entity Framework Commands

### Install EF Tools (if not installed):
```bash
dotnet tool install --global dotnet-ef
```

### Drop Database:
```bash
dotnet ef database drop --project Infrastructure --startup-project ECommerce.Server --force
```

This will:
- Drop the entire database
- Remove all tables
- Remove migration history

**⚠️ WARNING**: This deletes ALL data!

## 🗑️ Method 3: Manual SQL Commands

If you prefer to run commands manually in SSMS:

```sql
USE [EcommerceAdmin]
GO

-- 1. Disable foreign key constraints
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) 
    + '.' + QUOTENAME(OBJECT_NAME(parent_object_id)) 
    + ' NOCHECK CONSTRAINT ' + QUOTENAME(name) + ';'
FROM sys.foreign_keys;
EXEC sp_executesql @sql;

-- 2. Drop all user tables
DECLARE @dropTables NVARCHAR(MAX) = N'';
SELECT @dropTables += N'DROP TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) 
    + '.' + QUOTENAME(name) + ';'
FROM sys.tables
WHERE name NOT LIKE 'sys%' AND name NOT LIKE 'MS_%';
EXEC sp_executesql @dropTables;

-- 3. Drop migration history
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') AND type in (N'U'))
    DROP TABLE [dbo].[__EFMigrationsHistory];
```

## ✅ After Cleaning Database

Once the database is clean:

1. **Create a fresh migration**:
   ```bash
   dotnet ef migrations add InitialIdentityAndPermissions --project Infrastructure --startup-project ECommerce.Server
   ```

2. **Apply the migration**:
   ```bash
   dotnet ef database update --project Infrastructure --startup-project ECommerce.Server
   ```

3. **Verify**: Check that only these tables exist:
   - `User` (Identity)
   - `Role` (Identity)
   - `UserRoles` (Identity)
   - `UserClaims` (Identity)
   - `RoleClaims` (Identity)
   - `UserLogins` (Identity)
   - `UserTokens` (Identity)
   - `Permissions` (Custom)
   - `RolePermissions` (Custom)
   - `__EFMigrationsHistory` (EF Core)

## 🔍 Verification Checklist

After cleanup, verify:
- ✅ No Product table exists
- ✅ No Category table exists
- ✅ No old Identity tables with wrong names
- ✅ `__EFMigrationsHistory` is empty or removed
- ✅ You can create a fresh migration without conflicts

## ⚠️ Important Warnings

1. **BACKUP FIRST**: If you have important data, backup your database before cleaning
2. **Development Only**: This is typically done in development environments
3. **Production**: Use proper migration strategies for production
4. **Data Loss**: All data will be permanently deleted

## 🐛 Troubleshooting

### Error: "Cannot drop table because it is referenced by a foreign key"
**Solution**: The script handles this by disabling constraints first. If it still fails, manually drop foreign keys.

### Error: "Migration already exists" when creating new migration
**Solution**: The migration history still exists. Make sure `__EFMigrationsHistory` table is dropped.

### Error: "Database does not exist"
**Solution**: Create the database first:
```sql
CREATE DATABASE [EcommerceAdmin]
```

## 📝 Quick Reference

**Database Name**: `EcommerceAdmin`  
**Server**: `DESKTOP-301RNP6\SQL20222`  
**SQL Script**: `CleanDatabase.sql` (in project root)
