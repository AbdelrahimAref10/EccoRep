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
  ├── Query/
  │   └── [Action]Query/
  │       └── [Action]Query.cs (contains Query and Handler)
  └── DTOs/
      └── [Entity]Dto.cs (shared DTOs for the feature)
```

### Feature Organization by User Type
For applications with multiple user types (Customer, Admin), organize features by user type:

```
Application/Features/
  ├── Customer/
  │   ├── Command/
  │   │   ├── RegisterCustomerCommand/
  │   │   ├── ActivateCustomerCommand/
  │   │   └── CustomerLoginCommand/
  │   ├── Query/
  │   └── DTOs/
  │       └── CustomerDto.cs
  ├── Admin/
  │   ├── Command/
  │   │   └── AdminLoginCommand/
  │   ├── Query/
  │   └── DTOs/
  └── Roles/  (shared features)
      ├── Command/
      └── Query/
```

**Guidelines**:
- **Customer features**: All customer-specific commands/queries go in `Features/Customer/`
- **Admin features**: All admin-specific commands/queries go in `Features/Admin/`
- **Shared features**: Features used by multiple user types go in their own folders (e.g., `Features/Roles/`)
- **DTOs**: Create DTOs folder within each feature for feature-specific DTOs

### Response Patterns
- Use `Result<T>` from CSharpFunctionalExtensions for domain operations
- **Add/Create Commands**: Must return `Result<int>` (ID of created entity)
- **Update Commands**: Must return `Result<int>` (ID of updated entity)
- **Delete Commands**: Return `Result<bool>` (success/failure)
- **Queries**: Return `Result<T>` where T is the response DTO
- Always include error handling in responses
- Use `Result.Failure<T>(errorMessage)` for errors
- Use `Result.Success(value)` for successful operations
- **Rule**: Add and Update commands should return only the entity ID, not the full DTO

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

### Validation Architecture Rule
**MANDATORY**: All Command validation logic MUST be separated into a dedicated validator class in a separate file.

### Validation File Structure
- **File Naming**: `[CommandName]Validator.cs` (e.g., `AdminCreateCustomerCommandValidator.cs`)
- **Location**: Same folder as the command (e.g., `Application/Features/Customer/Command/AdminCreateCustomerCommand/AdminCreateCustomerCommandValidator.cs`)
- **Class Naming**: `[CommandName]Validator` (e.g., `AdminCreateCustomerCommandValidator`)

### Validator Implementation Pattern
- Validators must be separate classes (not in the command handler file)
- Validators should be registered in DI container (Scoped lifetime)
- Validators return `Result` from CSharpFunctionalExtensions
- Validators handle all input validation and business rule validation
- Command handlers call validators at the start of `Handle` method
- Command handlers should NOT contain validation logic

### Validator Example
```csharp
// AdminCreateCustomerCommandValidator.cs (separate file)
public class AdminCreateCustomerCommandValidator
{
    private readonly DatabaseContext _context;

    public AdminCreateCustomerCommandValidator(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<Result> ValidateAsync(AdminCreateCustomerCommand request, CancellationToken cancellationToken)
    {
        // All validation logic here
        if (string.IsNullOrWhiteSpace(request.MobileNumber))
        {
            return Result.Failure("Mobile number is required");
        }
        // ... more validations
        return Result.Success();
    }
}

// AdminCreateCustomerCommand.cs (command handler)
public class AdminCreateCustomerCommandHandler : IRequestHandler<AdminCreateCustomerCommand, Result<int>>
{
    private readonly AdminCreateCustomerCommandValidator _validator;
    
    public async Task<Result<int>> Handle(AdminCreateCustomerCommand request, CancellationToken cancellationToken)
    {
        // Validate first
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result.Failure<int>(validationResult.Error);
        }
        
        // Business logic only (no validation)
        // ...
    }
}
```

### Validation Responsibilities
- **Validators**: Handle all validation (required fields, format validation, uniqueness checks, business rules)
- **Command Handlers**: Focus only on business logic (create, update, delete operations)
- **Domain Entities**: Validate their own internal state through factory methods and domain methods

### Registration
- Register all validators in `ApplicationServiceRegistration.cs`
- Use Scoped lifetime (matches DatabaseContext lifetime)
- Example:
  ```csharp
  services.AddScoped<Features.Customer.Command.AdminCreateCustomerCommand.AdminCreateCustomerCommandValidator>();
  ```

### Business Rule Validation
- Implement business rules in domain entities
- Use domain services for complex business validation
- Return meaningful error messages
- Use Result pattern for validation results
- Validate before persisting to database

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
- Use JWT tokens for API authentication
- Include roles in JWT token claims
- Configure token expiration appropriately

### JWT Token Configuration
- Store JWT settings in `appsettings.json`
- Use secure key (minimum 32 characters for HS256)
- Set appropriate expiration times
- Include user ID, username, email, and roles in token
- Validate token on each authenticated request

### Authorization
- Use role-based authorization
- Apply `[Authorize]` attributes on controllers/actions
- Use `[Authorize(Roles = "Admin")]` for role-based access
- Use `[AllowAnonymous]` for public endpoints (login, register)
- Implement policy-based authorization for complex scenarios

### User Session
- Use `IUserSession` interface to access current user information
- Implement `UserSession` service to extract claims from JWT token
- Access user ID, username, email, and roles through `IUserSession`
- Register `IUserSession` as Scoped service

### Data Protection
- Never expose sensitive data in responses
- Use HTTPS in production
- Validate all user inputs
- Protect against SQL injection (use parameterized queries)
- Never return invitation codes in production responses

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

## 15. Frontend Architecture (Angular)

### Angular Rules
- See **ANGULAR_RULES.md** for complete Angular architecture guidelines
- All components must be standalone
- Use Tailwind CSS with BEM naming convention
- Use dynamic colors from Tailwind config (main-*, error-*, success-*)
- Create reusable components in `shared/components/`
- Never edit auto-generated `clientAPI.ts` file

## 14. Frontend Integration (Angular)

### NSwag Code Generation
- **NSwag** is used to automatically generate TypeScript client services from the backend API
- The generated service file is located at: `ecommerce.client/src/app/core/services/clientAPI.ts`
- **Auto-generation**: After building the backend, NSwag automatically generates the client service
- **Configuration**: NSwag configuration is in `ECommerce.Server/nswag.json`
- **Build Process**: The service is regenerated automatically during the build process (PostBuild event)

### Using Generated Services
- **Never manually edit** `clientAPI.ts` - it's auto-generated and will be overwritten
- All API clients are available as injectable services:
  - `AdminClient` - Admin operations (login, etc.)
  - `CustomerClient` - Customer operations (register, login, activate, refreshToken)
  - `RoleClient` - Role management operations
- Services are automatically provided in root (`providedIn: 'root'`)

### API Base URL Configuration
- Configure `API_BASE_URL` in `main.ts` using dependency injection
- Use the `API_BASE_URL` injection token from `clientAPI.ts`
- Example:
  ```typescript
  {
    provide: API_BASE_URL,
    useValue: 'https://localhost:7020'
  }
  ```

### Authentication Service Pattern
- Create a wrapper service (`AuthService`) that uses the generated clients
- Handle token storage in localStorage
- Implement login/logout methods
- Provide authentication state checking

### HTTP Interceptors
- Use Angular HTTP interceptors to automatically add JWT tokens to requests
- Interceptor should read token from `AuthService` and add to `Authorization` header
- Pattern: `Authorization: Bearer {token}`

### Route Guards
- Use `AuthGuard` to protect routes that require authentication
- Check authentication state before allowing route access
- Redirect to login page if not authenticated

### Form Validation
- Use Angular Reactive Forms for all forms
- Implement client-side validation with proper error messages
- Show validation errors inline with form fields
- Disable submit button during form submission

