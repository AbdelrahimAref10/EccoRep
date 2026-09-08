# Entity Mapping Configuration Summary

This document describes the entity mapping configuration pattern used in the ECommerce application.

## Overview

All entity configurations are implemented using the `IEntityTypeConfiguration<T>` interface pattern. Each entity has its own mapping configuration file in the `Infrastructure/MappingConfiguration` folder.

## Configuration Pattern

Each mapping configuration follows this pattern:

```csharp
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.MappingConfiguration
{
    public class [Entity]Configuration : IEntityTypeConfiguration<[Entity]>
    {
        public void Configure(EntityTypeBuilder<[Entity]> builder)
        {
            // Table name
            builder.ToTable("TableName");

            // Primary key
            builder.HasKey(e => e.Id);

            // Property configurations
            builder.Property(e => e.PropertyName)
                .HasColumnName("ColumnName")
                .HasMaxLength(256)
                .IsRequired();

            // Relationships
            builder.HasOne(...)
                .WithMany(...)
                .HasForeignKey(...);

            // Indexes
            builder.HasIndex(e => e.PropertyName);
        }
    }
}
```

## Mapping Files Created

### 1. ApplicationUserConfiguration.cs
- **Table**: `Users`
- **Primary Key**: `Id` (int)
- **Properties**: All Identity properties with column names and constraints
- **Audit Properties**: CreatedBy, CreatedDate, LastModifiedBy, LastModifiedDate
- **Indexes**: NormalizedUserName (unique), NormalizedEmail

### 2. ApplicationRoleConfiguration.cs
- **Table**: `Roles`
- **Primary Key**: `Id` (int)
- **Properties**: Name, NormalizedName, ConcurrencyStamp
- **Audit Properties**: CreatedBy, CreatedDate, LastModifiedBy, LastModifiedDate
- **Indexes**: NormalizedName (unique)

### 3. IdentityUserRoleConfiguration.cs
- **Table**: `UserRoles`
- **Primary Key**: Composite (UserId, RoleId)
- **Properties**: UserId, RoleId with column names

### 4. IdentityUserClaimConfiguration.cs
- **Table**: `UserClaims`
- **Primary Key**: `Id` (int, auto-generated)
- **Properties**: UserId, ClaimType, ClaimValue

### 5. IdentityUserLoginConfiguration.cs
- **Table**: `UserLogins`
- **Primary Key**: Composite (LoginProvider, ProviderKey)
- **Properties**: LoginProvider, ProviderKey, ProviderDisplayName, UserId

### 6. IdentityUserTokenConfiguration.cs
- **Table**: `UserTokens`
- **Primary Key**: Composite (UserId, LoginProvider, Name)
- **Properties**: UserId, LoginProvider, Name, Value

### 7. IdentityRoleClaimConfiguration.cs
- **Table**: `RoleClaims`
- **Primary Key**: `Id` (int, auto-generated)
- **Properties**: RoleId, ClaimType, ClaimValue

### 8. CategoryConfiguration.cs (Updated)
- **Table**: `Category`
- **Primary Key**: `CategoryId`
- **Properties**: CategoryId, CategoryName with column names and constraints
- **Audit Properties**: CreatedBy, CreatedDate, LastModifiedBy, LastModifiedDate
- **Relationships**: One-to-many with Products

### 9. ProductMapping.cs (Updated)
- **Table**: `Products`
- **Primary Key**: `ProductId`
- **Properties**: ProductId, ProductName, ProductDescription, Price, ImageUrl, CategoryId
- **Audit Properties**: CreatedBy, CreatedDate, LastModifiedBy, LastModifiedDate
- **Relationships**: Many-to-one with Category
- **Data Types**: Price as decimal(18,2)

## DatabaseContext Configuration

The `DatabaseContext` uses `ApplyConfigurationsFromAssembly` to automatically discover and apply all `IEntityTypeConfiguration` implementations:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Apply all entity configurations from the assembly
    // This will automatically pick up all IEntityTypeConfiguration implementations
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(DatabaseContext).Assembly);
    
    // Seed data
    modelBuilder.Entity<Category>().HasData(new Category
    {
        CategoryId = 1,
        CategoryName = "Male weare"
    });
}
```

## Benefits of This Approach

1. **Separation of Concerns**: Each entity's configuration is in its own file
2. **Maintainability**: Easy to find and update entity configurations
3. **Consistency**: All configurations follow the same pattern
4. **Automatic Discovery**: No need to manually register each configuration
5. **Type Safety**: Compile-time checking of entity configurations
6. **Testability**: Configurations can be tested independently

## Configuration Guidelines

### Property Configuration
- Always specify `HasColumnName` for explicit column naming
- Use `HasMaxLength` for string properties
- Use `IsRequired` for non-nullable properties
- Use `HasDefaultValue` for properties with default values
- Use `HasColumnType` for specific database types (e.g., `decimal(18,2)`)

### Relationship Configuration
- Use `HasOne`/`WithMany` for one-to-many relationships
- Use `HasMany`/`WithOne` for many-to-one relationships
- Specify `OnDelete` behavior (Restrict, Cascade, SetNull)
- Always configure foreign keys explicitly

### Index Configuration
- Create indexes for frequently queried columns
- Use unique indexes where appropriate
- Add filters for nullable unique indexes: `.HasFilter("[ColumnName] IS NOT NULL")`

### Audit Properties
- All entities implementing `IAuditable` should have audit properties configured
- Properties: CreatedBy, CreatedDate, LastModifiedBy, LastModifiedDate
- CreatedDate and LastModifiedDate should be `IsRequired`
- CreatedBy and LastModifiedBy should allow nulls (for system-generated records)

## Adding New Entity Mappings

When adding a new entity:

1. Create a new configuration file: `[Entity]Configuration.cs`
2. Implement `IEntityTypeConfiguration<[Entity]>`
3. Configure table name, primary key, properties, relationships, and indexes
4. The configuration will be automatically discovered by `ApplyConfigurationsFromAssembly`

Example:
```csharp
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.OrderId);
        // ... rest of configuration
    }
}
```

## Migration Considerations

When creating migrations after adding/updating configurations:

1. Review the generated migration to ensure it matches expectations
2. Test the migration in a development environment first
3. Verify that all column names, types, and constraints are correct
4. Check that relationships and indexes are properly created

## Best Practices

1. **One Configuration Per Entity**: Keep each entity's configuration in a separate file
2. **Consistent Naming**: Use `[Entity]Configuration` naming convention
3. **Documentation**: Add comments for complex configurations
4. **Validation**: Ensure all required properties are marked as `IsRequired`
5. **Performance**: Add indexes for frequently queried columns
6. **Data Integrity**: Use appropriate `OnDelete` behaviors for relationships

