# Authentication and Authorization Rules

This document outlines the rules and guidelines for authentication and authorization in the ECommerce application using ASP.NET Core Identity with integer primary keys.

## 1. Identity Configuration

### Primary Key Type
- **Use integer (int) as primary key type**, not GUID
- All Identity tables use `int` as primary key:
  - `ApplicationUser.Id` → `int`
  - `ApplicationRole.Id` → `int`
  - `IdentityUserRole<int>`
  - `IdentityUserClaim<int>`
  - `IdentityRoleClaim<int>`
  - `IdentityUserLogin<int>`
  - `IdentityUserToken<int>`

### Identity Models
- **ApplicationUser**: Extends `IdentityUser<int>` and implements `IAuditable`
- **ApplicationRole**: Extends `IdentityRole<int>` and implements `IAuditable`
- Both models inherit audit properties from `IAuditable` interface

### Database Context
- Inherit from `IdentityDbContext<ApplicationUser, ApplicationRole, int>`
- Configure Identity tables with custom table names
- Ensure proper foreign key relationships

## 2. Identity Setup

### Service Registration
Identity services must be registered in the Infrastructure layer:

```csharp
services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<DatabaseContext>()
.AddDefaultTokenProviders();
```

### Password Policy
- Minimum length: 8 characters
- Require digit: Yes
- Require uppercase: Yes
- Require lowercase: Yes
- Require non-alphanumeric: No (configurable)

### User Settings
- Require unique email: Yes
- Require confirmed email: No (configurable per environment)

### Lockout Policy
- Default lockout time: 5 minutes
- Max failed attempts: 5
- Lockout enabled for new users: Yes

## 3. Authentication

### Authentication Middleware
- Use `app.UseAuthentication()` before `app.UseAuthorization()`
- Place authentication middleware in the correct order in the pipeline
- Configure authentication schemes if using multiple providers

### JWT Token Configuration (if implemented)
- Store JWT settings in `appsettings.json`
- Use secure key storage (Azure Key Vault, environment variables)
- Set appropriate token expiration times
- Implement token refresh mechanism

### Cookie Authentication (if implemented)
- Configure secure cookie settings
- Use HttpOnly cookies
- Set SameSite policy appropriately
- Configure cookie expiration

## 4. Authorization

### Role-Based Authorization
- Use `[Authorize(Roles = "RoleName")]` attribute for role-based access
- Support multiple roles: `[Authorize(Roles = "Admin,Manager")]`
- Apply authorization at controller or action level
- Use `User.IsInRole("RoleName")` for programmatic checks

### Policy-Based Authorization
- Create policies for complex authorization scenarios
- Register policies in `Program.cs` or `Startup.cs`
- Use `[Authorize(Policy = "PolicyName")]` attribute

### Claims-Based Authorization
- Use claims for fine-grained authorization
- Add claims during user creation or login
- Check claims using `User.HasClaim("Type", "Value")`

### Authorization Levels
1. **Public**: No authorization required
2. **Authenticated**: User must be logged in (`[Authorize]`)
3. **Role-Based**: User must have specific role (`[Authorize(Roles = "Admin")]`)
4. **Policy-Based**: User must satisfy policy (`[Authorize(Policy = "PolicyName")]`)

## 5. Role Management

### Role Operations
All role operations follow CQRS pattern:

#### Commands
- **CreateRoleCommand**: Create a new role
- **UpdateRoleCommand**: Update an existing role
- **DeleteRoleCommand**: Delete a role
- **AssignRoleToUserCommand**: Assign role to user
- **RemoveRoleFromUserCommand**: Remove role from user

#### Queries
- **GetAllRolesQuery**: Get all roles
- **GetRoleByIdQuery**: Get role by ID
- **GetUserRolesQuery**: Get all roles for a user

### Role Naming Conventions
- Use PascalCase for role names (e.g., "Admin", "Manager", "Customer")
- Keep role names descriptive and clear
- Avoid special characters in role names
- Use consistent naming across the application

### Default Roles
Consider creating default roles:
- **Admin**: Full system access
- **Manager**: Management-level access
- **Customer**: Standard user access
- **Guest**: Limited access

### Role Assignment Rules
- Only users with "Admin" role can assign/remove roles
- Validate role exists before assignment
- Check if user already has role before assigning
- Validate user exists before role operations
- Return meaningful error messages for failures

## 6. User Management

### User Operations
User management operations should follow CQRS pattern:

#### Commands (to be implemented)
- **CreateUserCommand**: Register a new user
- **UpdateUserCommand**: Update user information
- **DeleteUserCommand**: Delete a user
- **ChangePasswordCommand**: Change user password
- **ResetPasswordCommand**: Reset user password

#### Queries (to be implemented)
- **GetUserByIdQuery**: Get user by ID
- **GetAllUsersQuery**: Get all users (paginated)
- **GetUserByEmailQuery**: Get user by email

### User Validation
- Validate email format
- Validate password strength
- Check for duplicate emails
- Validate required fields
- Return clear validation error messages

## 7. Security Best Practices

### Password Security
- Never store passwords in plain text
- Use Identity's password hashing (bcrypt)
- Implement password reset flow
- Enforce password complexity rules
- Consider password expiration policies

### Token Security
- Use secure token generation
- Implement token expiration
- Store tokens securely (HttpOnly cookies or secure storage)
- Implement token refresh mechanism
- Revoke tokens on logout

### API Security
- Use HTTPS in production
- Validate all inputs
- Protect against CSRF attacks
- Implement rate limiting
- Log security events

### Data Protection
- Never expose sensitive data in API responses
- Hash sensitive data at rest
- Use encryption for sensitive fields
- Implement data masking for logs
- Follow GDPR/privacy regulations

## 8. Authorization in Controllers

### Controller-Level Authorization
```csharp
[Authorize]
[Route("api/[controller]")]
public class RoleController : ControllerBase
{
    // All actions require authentication
}
```

### Action-Level Authorization
```csharp
[HttpPost]
[Authorize(Roles = "Admin")]
[Route("CreateRole")]
public async Task<IActionResult> CreateRole(CreateRoleCommand command)
{
    // Only Admin role can access
}
```

### Multiple Roles
```csharp
[Authorize(Roles = "Admin,Manager")]
public async Task<IActionResult> SomeAction()
{
    // Admin or Manager can access
}
```

## 9. Programmatic Authorization

### Check User Roles
```csharp
if (User.IsInRole("Admin"))
{
    // Admin-specific logic
}
```

### Check Claims
```csharp
if (User.HasClaim("Permission", "CanEditProducts"))
{
    // Permission-specific logic
}
```

### Get Current User
```csharp
var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
var user = await _userManager.FindByIdAsync(userId);
```

## 10. Audit Trail

### User Audit Fields
- **CreatedBy**: User who created the record
- **CreatedDate**: When the record was created
- **LastModifiedBy**: User who last modified the record
- **LastModifiedDate**: When the record was last modified

### Implementation
- All Identity entities implement `IAuditable`
- Set audit fields automatically in `SaveChangesAsyncWithResult`
- Track user ID from `HttpContext.User`
- Use UTC for all timestamps

## 11. Error Handling

### Authentication Errors
- Return 401 Unauthorized for authentication failures
- Return clear error messages
- Don't expose user existence information
- Log authentication failures

### Authorization Errors
- Return 403 Forbidden for authorization failures
- Return appropriate error messages
- Log authorization failures
- Don't expose sensitive information

### Validation Errors
- Return 400 Bad Request for validation failures
- Include detailed validation error messages
- Use consistent error response format
- Return all validation errors at once

## 12. Testing Authentication/Authorization

### Unit Testing
- Mock `UserManager` and `RoleManager`
- Test authorization logic
- Test role assignment/removal
- Test validation rules

### Integration Testing
- Test authentication flow
- Test authorization policies
- Test role-based access
- Test API endpoints with different roles

## 13. Migration and Seeding

### Initial Data
- Seed default roles on application startup
- Create default admin user (if needed)
- Use migrations for schema changes
- Use seed data for development/testing

### Role Seeding Example
```csharp
if (!await roleManager.RoleExistsAsync("Admin"))
{
    await roleManager.CreateAsync(new ApplicationRole { Name = "Admin" });
}
```

## 14. Compliance and Regulations

### GDPR Compliance
- Implement user data deletion
- Provide data export functionality
- Implement consent management
- Log data access

### Security Standards
- Follow OWASP guidelines
- Implement security headers
- Regular security audits
- Penetration testing

## 15. Monitoring and Logging

### Security Events
- Log all authentication attempts
- Log authorization failures
- Log role changes
- Log password changes
- Monitor for suspicious activity

### Audit Logging
- Log all user management operations
- Log role assignments/removals
- Log permission changes
- Maintain audit trail for compliance

