# ECommerce Application - Architecture and Implementation Instructions

This folder contains all architecture rules, implementation guidelines, and documentation for the ECommerce application.

## 📚 Documentation Index

### Architecture & Design Rules

1. **ARCHITECTURE_RULES.md**
   - Clean Architecture layer structure
   - CQRS implementation guidelines
   - Domain-Driven Design principles
   - Data access patterns
   - Dependency injection rules
   - Validation rules
   - Exception handling
   - API design guidelines
   - Security best practices

2. **AUTHENTICATION_AUTHORIZATION_RULES.md**
   - Identity configuration with integer primary keys
   - Authentication setup and configuration
   - Authorization patterns (Role-based, Policy-based, Claims-based)
   - Role management guidelines
   - User management guidelines
   - Security best practices
   - Audit trail implementation

### Implementation Documentation

3. **IMPLEMENTATION_SUMMARY.md**
   - Complete summary of role management implementation
   - Identity models with integer primary keys
   - Role management features (Commands and Queries)
   - Global exception handling middleware
   - Database context updates
   - API endpoints documentation

4. **MAPPING_CONFIGURATION_SUMMARY.md**
   - Entity mapping configuration pattern
   - All mapping files created
   - Configuration guidelines
   - Best practices for entity mappings
   - How to add new entity mappings

5. **PERMISSIONS_IMPLEMENTATION.md**
   - Permissions and RolePermissions entities
   - Database table structures
   - Relationships and foreign keys
   - Usage examples
   - Migration commands

### Identity & Tables Documentation

6. **IDENTITY_TABLES_EXPLANATION.md**
   - Complete explanation of all Identity tables
   - What Identity creates automatically
   - What's missing (custom tables)
   - Relationships between tables
   - Foreign key configurations

7. **CURRENT_IDENTITY_SETUP.md**
    - Current state of Identity implementation
    - What exists and what's missing
    - Table structures
    - Relationship diagrams
    - Next steps for implementation

8. **ANGULAR_RULES.md**
    - Angular architecture and best practices
    - Component structure and organization
    - Tailwind CSS with BEM methodology
    - NSwag integration guidelines
    - Form validation patterns
    - Service and dependency injection rules
    - Routing and guards
    - State management
    - Error handling

## 🎯 Quick Reference

### For Developers

- **Starting a new feature?** → Read `ARCHITECTURE_RULES.md`
- **Working with Identity?** → Read `AUTHENTICATION_AUTHORIZATION_RULES.md`
- **Creating new entities?** → Read `MAPPING_CONFIGURATION_SUMMARY.md`
- **Understanding permissions?** → Read `PERMISSIONS_IMPLEMENTATION.md`
- **Setting up Identity?** → Read `IDENTITY_TABLES_EXPLANATION.md`
- **Working with Angular?** → Read `ANGULAR_RULES.md`

### For Architects

- **Reviewing architecture?** → Read `ARCHITECTURE_RULES.md`
- **Understanding current setup?** → Read `CURRENT_IDENTITY_SETUP.md`
- **Planning new features?** → Read `IMPLEMENTATION_SUMMARY.md`

## 📋 Key Principles

1. **Clean Architecture**: Strict layer separation
2. **CQRS**: Commands and Queries separation using MediatR
3. **DDD**: Domain-driven design with rich domain models
4. **Integer Primary Keys**: All Identity tables use int, not GUID
5. **Audit Trail**: All entities implement IAuditable
6. **Mapping Configurations**: Each entity has its own mapping file
7. **Result Pattern**: Use CSharpFunctionalExtensions Result<T> for error handling

## 🔄 Keeping Documentation Updated

When making changes to the codebase:
1. Update relevant documentation files
2. Follow the patterns and rules documented here
3. Add examples if introducing new patterns
4. Keep relationship diagrams updated

## 📝 Document Structure

Each document follows this structure:
- Overview/Introduction
- Detailed explanations
- Code examples
- Best practices
- Next steps

## 🚀 Getting Started

1. Read `ARCHITECTURE_RULES.md` for overall architecture
2. Read `AUTHENTICATION_AUTHORIZATION_RULES.md` for auth setup
3. Review `CURRENT_IDENTITY_SETUP.md` for current state
4. Follow patterns in `IMPLEMENTATION_SUMMARY.md` for new features

---

**Last Updated**: 2024
**Version**: 1.0

