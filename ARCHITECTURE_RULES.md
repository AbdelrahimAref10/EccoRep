# Architecture Rules

This document outlines the architectural rules and guidelines for the ECommerce application following Clean Architecture, CQRS, and DDD principles.

## 1. Clean Architecture Layers

### Layer Structure
The application follows Clean Architecture with the following layers:

- **Domain**: Core business logic, entities, value objects, domain services
- **Application**: Use cases, commands, queries, handlers, DTOs
- **Infrastructure**: Data access, external services, Identity configuration
- **Presentation**: Controllers, middleware, response models
- **ECommerce.Server**: API entry point, startup configuration

### Dependency Rules
- **Domain**: No dependencies on other layers (pure business logic)
- **Application**: Depends only on Domain
- **Infrastructure**: Depends on Domain and Application
- **Presentation**: Depends on Application and Domain
- **ECommerce.Server**: Depends on all layers

### Layer Responsibilities

#### Domain Layer
- Contains business entities and value objects
- Implements business rules and domain logic
- No external dependencies (except CSharpFunctionalExtensions for Result pattern)
- Entities inherit from `AuditableEntity` or implement `IAuditable`
- Use factory methods for complex object creation
- Encapsulate business logic in domain methods

#### Application Layer
- Contains use cases (Commands and Queries)
- Implements CQRS pattern using MediatR
- Handles validation and orchestration
- Returns `Result<T>` from CSharpFunctionalExtensions
- No direct database access (uses Infrastructure through interfaces)

#### Infrastructure Layer
- Implements data access using Entity Framework Core
- Configures database context and Identity
- Implements repository pattern (if needed)
- Handles migrations and database configuration
- Implements Unit of Work pattern through DbContext

#### Presentation Layer
- Contains API controllers
- Handles HTTP requests and responses
- Implements middleware (exception handling, logging)
- Maps between DTOs and domain models

## 2. CQRS (Command Query Responsibility Segregation)

### Implementation Rules
- Use MediatR for CQRS implementation
- Commands and Queries must be separate classes
- Each Command/Query must have a corresponding Handler
- Use `IRequest<T>` interface for Commands/Queries
- Use `IRequestHandler<TRequest, TResponse>` for Handlers

### Naming Conventions
- **Commands**: `[Action]Command` (e.g., `CreateRoleCommand`, `UpdateProductCommand`)
- **Queries**: `[Action]Query` (e.g., `GetAllRolesQuery`, `GetProductByIdQuery`)
- **Handlers**: `[Command/Query]Handler` (e.g., `CreateRoleCommandHandler`, `GetAllRolesQueryHandler`)
- **View Models**: `[Entity]Vm` (e.g., `RoleVm`, `ProductVm`)

### File Structure
```
Application/Features/[FeatureName]/
  ├── Command/
  │   └── [Action]Command/
  │       └── [Action]Command.cs (contains Command and Handler)
  └── Query/
      └── [Action]Query/
          └── [Action]Query.cs (contains Query and Handler)
```

### Response Patterns
- Use `Result<T>` from CSharpFunctionalExtensions for domain operations
- Commands typically return `Result<int>` (ID of created entity) or `Result<bool>`
- Queries return `Result<T>` where T is the response DTO
- Always include error handling in responses
- Use `Result.Failure<T>(errorMessage)` for errors
- Use `Result.Success(value)` for successful operations

### Handler Guidelines
- Keep handlers focused on a single responsibility
- Use dependency injection for services
- Handle business rule validation
- Return meaningful error messages
- Use async/await for all database operations

## 3. Domain-Driven Design (DDD)

### Entity Rules
- Domain entities must inherit from `AuditableEntity` or implement `IAuditable` interface
- Use private setters for encapsulation
- Business logic must be encapsulated in domain methods
- Use factory methods (`Instance`, `Create`) for complex object creation
- Entities should validate their own state
- Avoid anemic domain models (entities with only properties)

### Value Objects
- Use value objects for concepts without identity
- Make value objects immutable
- Implement proper equality comparison
- Use value objects to encapsulate business rules

### Domain Services
- Use domain services for business logic that doesn't belong to entities
- Keep domain services stateless
- Inject dependencies through constructor
- Domain services should be in the Domain layer

### Aggregate Roots
- Identify aggregate roots for consistency boundaries
- Access child entities only through aggregate root
- Maintain referential integrity within aggregates

## 4. Data Access

### Implementation Rules
- Repository implementations in Infrastructure layer
- Use Entity Framework Core for data access
- Implement Unit of Work pattern for transactions (DbContext acts as UoW)
- Use specification pattern for complex queries
- Use `AsTracking()` for entities that need to be modified
- Use `AsNoTracking()` for read-only queries

### Database Context
- Inherit from `IdentityDbContext<ApplicationUser, ApplicationRole, int>` for Identity support
- Configure entities using Fluent API in `OnModelCreating`
- Use `ApplyConfigurationsFromAssembly` for entity configurations
- Implement `SaveChangesAsyncWithResult` for consistent error handling

### Migration Guidelines
- Create migrations for all schema changes
- Review migration files before applying
- Use meaningful migration names
- Test migrations in development before production

## 5. Dependency Injection

### Registration Rules
- Register all services in `Startup.cs` or `Program.cs` (via extension methods)
- Use appropriate lifetime (Singleton, Scoped, Transient)
- Register interfaces, not concrete implementations
- Use Scrutor for convention-based registration (if needed)

### Service Lifetime Guidelines
- **Singleton**: Stateless services, configuration, logging
- **Scoped**: Database contexts, business services, UserManager, RoleManager
- **Transient**: Lightweight services, factories, validators

### Registration Pattern
```csharp
// In Infrastructure layer
public static IServiceCollection AddDatabaseServices(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    services.AddDbContext<DatabaseContext>(...);
    services.AddIdentity<ApplicationUser, ApplicationRole>(...);
    return services;
}

// In Application layer
public static IServiceCollection AddApplicationServices(
    this IServiceCollection services)
{
    services.AddMediatR(...);
    return services;
}
```

## 6. Validation Rules

### Input Validation
- Use FluentValidation for all input validation
- Create validators for each Command/Query
- Implement validation behavior as MediatR pipeline behavior
- Validate at the application layer boundary
- Return validation errors in a consistent format

### Business Rule Validation
- Implement business rules in domain entities
- Use domain services for complex business validation
- Return meaningful error messages
- Use Result pattern for validation results
- Validate before persisting to database

### Validation Example
```csharp
// In Application layer
public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role name is required")
            .MaximumLength(50).WithMessage("Role name must not exceed 50 characters");
    }
}
```

## 7. Exception Handling

### Global Exception Handling
- Use global exception handling middleware
- Log all exceptions with structured logging
- Return consistent error response format
- Don't expose internal implementation details

### Exception Handling Middleware
- Place middleware early in the pipeline (before routing)
- Catch all unhandled exceptions
- Log exceptions with context (RequestId, UserId, etc.)
- Return appropriate HTTP status codes
- Include stack traces only in development environment

### Error Response Format
```json
{
  "statusCode": 400,
  "message": "Error message",
  "requestId": "trace-id",
  "timestamp": "2024-01-01T00:00:00Z",
  "details": "Stack trace (development only)"
}
```

## 8. Logging

### Structured Logging
- Use ILogger<T> for all logging
- Use structured logging with parameters
- Log at appropriate levels (Error, Warning, Information, Debug)
- Include context in log messages (RequestId, UserId, etc.)
- Don't log sensitive information (passwords, tokens)

### Logging Levels
- **Error**: Exceptions, failures, critical issues
- **Warning**: Recoverable issues, validation failures
- **Information**: Important business events, request/response
- **Debug**: Detailed diagnostic information

## 9. API Design

### Controller Guidelines
- Keep controllers thin (delegate to MediatR)
- Use appropriate HTTP verbs (GET, POST, PUT, DELETE)
- Return proper HTTP status codes
- Use route attributes for clean URLs
- Apply authorization attributes where needed

### Response Types
- Use `ProducesResponseType` attributes for API documentation
- Return `ProblemDetail` for errors
- Return DTOs (View Models) for successful responses
- Use consistent response formats

### API Versioning
- Consider API versioning for future changes
- Use version in route or header
- Maintain backward compatibility when possible

## 10. Security

### Authentication
- Use ASP.NET Core Identity for authentication
- Configure password policies
- Implement lockout policies
- Use secure token providers

### Authorization
- Use role-based authorization
- Apply `[Authorize]` attributes on controllers/actions
- Use `[Authorize(Roles = "Admin")]` for role-based access
- Implement policy-based authorization for complex scenarios

### Data Protection
- Never expose sensitive data in responses
- Use HTTPS in production
- Validate all user inputs
- Protect against SQL injection (use parameterized queries)

## 11. Testing

### Unit Testing
- Test domain logic in isolation
- Test command/query handlers
- Mock external dependencies
- Test edge cases and error scenarios

### Integration Testing
- Test database operations
- Test API endpoints
- Test authentication/authorization
- Use test databases

## 12. Code Organization

### Namespace Conventions
- Follow .NET namespace conventions
- Use feature-based organization in Application layer
- Group related classes together
- Use clear, descriptive namespaces

### File Organization
- One class per file
- Match file name to class name
- Group related files in folders
- Use consistent folder structure

## 13. Best Practices

### General
- Follow SOLID principles
- Write self-documenting code
- Use meaningful names
- Keep methods small and focused
- Avoid deep nesting
- Use async/await for I/O operations
- Handle nulls properly (use nullable reference types)

### Performance
- Use async/await for database operations
- Use `AsNoTracking()` for read-only queries
- Implement pagination for large datasets
- Cache frequently accessed data when appropriate
- Optimize database queries

### Maintainability
- Write clear, concise code
- Add comments for complex logic
- Keep dependencies minimal
- Follow consistent patterns
- Refactor regularly

