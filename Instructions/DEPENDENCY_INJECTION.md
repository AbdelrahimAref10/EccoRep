# Dependency Injection Configuration

This document explains where and how services are registered in the ECommerce application.

## Service Registration Locations

### 1. Application Layer Services

**File**: `Application/ApplicationServiceRegistration.cs`

**What's Registered**:
- MediatR (CQRS pattern)
  - All Command and Query handlers are automatically discovered

**Registration Method**:
```csharp
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly()));
    return services;
}
```

**Service Lifetime**: 
- MediatR handlers are registered as **Transient**

### 2. Infrastructure Layer Services

**File**: `Infrastructure/DatabaseConfiguration.cs`

**What's Registered**:
- Entity Framework Core DbContext (`DatabaseContext`)
- ASP.NET Core Identity services:
  - `UserManager<ApplicationUser>`
  - `RoleManager<ApplicationRole>`
  - `SignInManager<ApplicationUser>`
  - Identity stores and token providers

**Registration Method**:
```csharp
public static IServiceCollection AddDatabaseServices(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    // Entity Framework Core
    services.AddDbContext<DatabaseContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
    );

    // ASP.NET Core Identity
    services.AddIdentity<ApplicationUser, ApplicationRole>(options => { ... })
        .AddEntityFrameworkStores<DatabaseContext>()
        .AddDefaultTokenProviders();

    return services;
}
```

**Service Lifetimes**:
- `DatabaseContext`: **Scoped**
- `UserManager<ApplicationUser>`: **Scoped**
- `RoleManager<ApplicationRole>`: **Scoped**
- `SignInManager<ApplicationUser>`: **Scoped**

### 3. Presentation Layer Services

**File**: `Presentation/Middleware/GlobalExceptionHandlingMiddleware.cs`

**What's Registered**:
- Global exception handling middleware (registered in pipeline, not DI)

**Registration**: Added to pipeline in `StatupExtensions.cs`

### 4. Main Service Registration

**File**: `ECommerce.Server/StatupExtensions.cs`

**Method**: `ConfigureServices`

**Registration Order**:
```csharp
public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
{
    // 1. Application services (MediatR, handlers)
    builder.Services.AddApplicationServices();
    
    // 2. Infrastructure services (DbContext, Identity)
    builder.Services.AddDatabaseServices(builder.Configuration);
    
    // 3. ASP.NET Core services
    builder.Services.AddControllers();
    builder.Services.AddCors(...);
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApiDocument(...);
    
    return builder.Build();
}
```

**Called From**: `ECommerce.Server/Program.cs`

```csharp
var app = builder
    .ConfigureServices()  // ← All services registered here
    .ConfigurePipeline();
```

## Service Lifetime Guidelines

### Singleton
- **Use for**: Stateless services, configuration, logging
- **Examples**: 
  - `ILogger<T>` (provided by framework)
  - Configuration services

### Scoped
- **Use for**: Database contexts, business services, Identity managers
- **Examples**:
  - `DatabaseContext`
  - `UserManager<ApplicationUser>`
  - `RoleManager<ApplicationRole>`
  - `SignInManager<ApplicationUser>`
  - Business services (if you add them)

### Transient
- **Use for**: Lightweight services, factories, handlers
- **Examples**:
  - MediatR handlers (Command/Query handlers)
  - Validators (if using FluentValidation)
  - Factory services

## Identity Services Available

After registration, these services are available for dependency injection:

### UserManager<ApplicationUser>
```csharp
public class SomeHandler : IRequestHandler<SomeCommand, Result>
{
    private readonly UserManager<ApplicationUser> _userManager;
    
    public SomeHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }
}
```

### RoleManager<ApplicationRole>
```csharp
public class SomeHandler : IRequestHandler<SomeCommand, Result>
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    
    public SomeHandler(RoleManager<ApplicationRole> roleManager)
    {
        _roleManager = roleManager;
    }
}
```

### SignInManager<ApplicationUser>
```csharp
public class SomeHandler : IRequestHandler<SomeCommand, Result>
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    
    public SomeHandler(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }
}
```

## Service Registration Pattern

The application follows a **layered registration pattern**:

1. **Domain Layer**: No services (pure business logic)
2. **Application Layer**: Business services (MediatR, handlers)
3. **Infrastructure Layer**: Data access services (DbContext, Identity)
4. **Presentation Layer**: Middleware, response models
5. **Server Layer**: Orchestrates all registrations

## Adding New Services

### Example: Adding a Repository Service

**Step 1**: Create interface in Domain or Application layer
```csharp
public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(int id);
}
```

**Step 2**: Create implementation in Infrastructure layer
```csharp
public class PermissionRepository : IPermissionRepository
{
    private readonly DatabaseContext _context;
    
    public PermissionRepository(DatabaseContext context)
    {
        _context = context;
    }
    
    public async Task<Permission?> GetByIdAsync(int id)
    {
        return await _context.Permissions.FindAsync(id);
    }
}
```

**Step 3**: Register in Infrastructure layer
```csharp
// In DatabaseConfiguration.cs or create new InfrastructureServiceRegistration.cs
public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
{
    services.AddScoped<IPermissionRepository, PermissionRepository>();
    return services;
}
```

**Step 4**: Call registration in StatupExtensions.cs
```csharp
builder.Services.AddInfrastructureServices();
```

## Current Service Registration Summary

| Service | Location | Lifetime | Registered In |
|---------|----------|----------|---------------|
| MediatR | Application | Transient | ApplicationServiceRegistration |
| DatabaseContext | Infrastructure | Scoped | DatabaseConfiguration |
| UserManager | Infrastructure | Scoped | DatabaseConfiguration |
| RoleManager | Infrastructure | Scoped | DatabaseConfiguration |
| SignInManager | Infrastructure | Scoped | DatabaseConfiguration |
| Controllers | Server | Scoped | StatupExtensions |
| CORS | Server | Singleton | StatupExtensions |
| OpenAPI | Server | Singleton | StatupExtensions |

## Best Practices

1. **Register in appropriate layer**: Keep services in their respective layers
2. **Use extension methods**: Create extension methods for service registration
3. **Follow naming convention**: `Add[Layer]Services` (e.g., `AddApplicationServices`)
4. **Register interfaces**: Register interfaces, not concrete implementations
5. **Choose correct lifetime**: Use appropriate lifetime for each service
6. **Order matters**: Register services in logical order (Application → Infrastructure → Server)

## Troubleshooting

### Error: "Service not registered"
- Check if service is registered in the correct extension method
- Verify the extension method is called in `StatupExtensions.cs`
- Ensure correct namespace is imported

### Error: "Cannot resolve service"
- Check service lifetime (Scoped services can't be injected into Singleton)
- Verify constructor parameters match registered services
- Check if service is registered before it's used

### Error: "AddIdentity not found"
- Ensure `Microsoft.AspNetCore.Identity` package is referenced in Infrastructure project
- Verify using statements include `Microsoft.AspNetCore.Identity`
- Check package version compatibility

