# Authentication Cycle Documentation

This document explains the complete authentication cycle for the ECommerce Scooter Rental application, from registration to login and token management.

## Overview

The application supports two types of users:
1. **Customer**: Can register, activate account with invitation code, and login
2. **Admin**: Pre-seeded user, can only login (no registration)

## Authentication Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    CUSTOMER AUTHENTICATION FLOW                 │
└─────────────────────────────────────────────────────────────────┘

1. REGISTRATION
   Customer → POST /api/Customer/Register
   ├── Input: MobileNumber, UserName, NationalNumber, Gender, Password
   ├── Creates ApplicationUser (Identity)
   ├── Assigns "Customer" role
   ├── Creates Customer record (IsActive = false)
   ├── Generates 6-digit invitation code
   ├── Sends code via SMS to mobile number
   └── Returns: CustomerId, InvitationCode (for testing), Message

2. ACTIVATION
   Customer → POST /api/Customer/Activate
   ├── Input: MobileNumber, InvitationCode
   ├── Validates invitation code
   ├── Checks code expiry (24 hours)
   ├── Sets IsActive = true
   ├── Marks code as used
   └── Returns: Success (true/false)

3. LOGIN
   Customer → POST /api/Customer/Login
   ├── Input: MobileNumber, Password
   ├── Validates credentials
   ├── Checks if customer is active
   ├── Generates JWT token with roles
   └── Returns: Token, UserId, UserName, Roles, CustomerId

┌─────────────────────────────────────────────────────────────────┐
│                     ADMIN AUTHENTICATION FLOW                   │
└─────────────────────────────────────────────────────────────────┘

1. LOGIN (No Registration)
   Admin → POST /api/Admin/Login
   ├── Input: UserName, Password
   ├── Validates credentials
   ├── Checks if user has "Admin" role
   ├── Generates JWT token with roles
   └── Returns: Token, UserId, UserName, Roles
```

## Detailed Step-by-Step Process

### Customer Registration Process

#### Step 1: Register Customer
**Endpoint**: `POST /api/Customer/Register`

**Request Body**:
```json
{
  "mobileNumber": "01234567890",
  "userName": "John Doe",
  "nationalNumber": "12345678901234",
  "gender": "Male",
  "password": "Customer@123"
}
```

**What Happens**:
1. **Validation**: Checks if mobile number and national number already exist
2. **Create Identity User**: 
   - Creates `ApplicationUser` with mobile number as username
   - Sets temporary email: `{mobileNumber}@customer.com`
   - Stores password (hashed by Identity)
3. **Assign Role**: Adds user to "Customer" role
4. **Create Customer Record**:
   - Creates `Customer` entity with provided data
   - Sets `IsActive = false`
   - Generates 6-digit invitation code
   - Sets code expiry to 24 hours from now
5. **Send SMS**: Invitation code sent to mobile number
6. **Response**: Returns CustomerId and invitation code (for testing)

**Code Location**: `Application/Features/Customer/Command/RegisterCustomerCommand/RegisterCustomerCommand.cs`

#### Step 2: Activate Customer
**Endpoint**: `POST /api/Customer/Activate`

**Request Body**:
```json
{
  "mobileNumber": "01234567890",
  "invitationCode": "123456"
}
```

**What Happens**:
1. **Find Customer**: Looks up customer by mobile number
2. **Validate Code**: 
   - Checks if code matches
   - Checks if code is already used
   - Checks if code is expired (24 hours)
3. **Activate**: 
   - Sets `IsActive = true`
   - Marks code as used
   - Clears invitation code
4. **Response**: Returns success/failure

**Code Location**: `Application/Features/Customer/Command/ActivateCustomerCommand/ActivateCustomerCommand.cs`

#### Step 3: Customer Login
**Endpoint**: `POST /api/Customer/Login`

**Request Body**:
```json
{
  "mobileNumber": "01234567890",
  "password": "Customer@123"
}
```

**What Happens**:
1. **Find User**: Looks up user by mobile number (username)
2. **Verify Role**: Ensures user has "Customer" role
3. **Check Activation**: Verifies customer is active
4. **Verify Password**: Identity validates password
5. **Generate Token**: Creates JWT token with:
   - UserId (NameIdentifier claim)
   - UserName (Name claim)
   - Email (Email claim)
   - Roles (Role claims)
6. **Response**: Returns JWT token and user information

**Code Location**: `Application/Features/Customer/Command/CustomerLoginCommand/CustomerLoginCommand.cs`

### Admin Login Process

#### Step 1: Admin Login
**Endpoint**: `POST /api/Admin/Login`

**Request Body**:
```json
{
  "userName": "admin",
  "password": "Admin@123"
}
```

**What Happens**:
1. **Find User**: Looks up user by username
2. **Verify Role**: Ensures user has "Admin" role
3. **Verify Password**: Identity validates password
4. **Generate Token**: Creates JWT token with roles
5. **Response**: Returns JWT token and user information

**Code Location**: `Application/Features/Admin/Command/AdminLoginCommand/AdminLoginCommand.cs`

**Note**: Admin user is seeded on application startup. No registration needed.

## JWT Token Structure

### Token Claims
```json
{
  "sub": "1",                    // UserId (NameIdentifier)
  "name": "admin",               // UserName
  "email": "admin@ecommerce.com", // Email
  "role": "Admin",               // Role claim (can be multiple)
  "jti": "guid",                 // JWT ID
  "exp": 1234567890,             // Expiration timestamp
  "iss": "ECommerceAPI",         // Issuer
  "aud": "ECommerceClient"       // Audience
}
```

### Token Usage
1. **Client stores token** (localStorage, sessionStorage, or secure cookie)
2. **Client sends token** in Authorization header: `Bearer {token}`
3. **Server validates token** on each authenticated request
4. **Server extracts user info** from token claims

## Using IUserSession

After authentication, you can access user information in any service/controller:

```csharp
public class SomeService
{
    private readonly IUserSession _userSession;

    public SomeService(IUserSession userSession)
    {
        _userSession = userSession;
    }

    public void DoSomething()
    {
        var userId = _userSession.UserId;        // Get current user ID
        var userName = _userSession.UserName;    // Get username
        var roles = _userSession.Roles;          // Get user roles
        var isAuth = _userSession.IsAuthenticated; // Check if authenticated
    }
}
```

**Code Location**: 
- Interface: `Domain/Common/IUserSession.cs`
- Implementation: `Infrastructure/Services/UserSession.cs`

## Database Tables Involved

### User Table (Identity)
- Stores: UserId, UserName, Email, PasswordHash, etc.
- Created by: Identity framework
- Used for: Authentication and authorization

### Customer Table
- Stores: CustomerId, MobileNumber, UserName, NationalNumber, Gender, IsActive, InvitationCode, etc.
- Created by: Application
- Linked to: User table via UserId foreign key

### Role Table (Identity)
- Stores: RoleId, Name (Admin, Customer)
- Created by: Identity framework
- Seeded: Admin and Customer roles

### UserRoles Table (Identity)
- Stores: UserId, RoleId
- Created by: Identity framework
- Links users to roles

## Seed Data

### Admin User
**Created on**: Application startup

**Location**: `Infrastructure/Data/SeedData.cs`

**Default Credentials**:
- Username: `admin`
- Password: `Admin@123`
- Role: `Admin`

**What Gets Created**:
1. Admin role (if not exists)
2. Customer role (if not exists)
3. Admin user with Admin role

## Security Considerations

### Password Policy
- Minimum 8 characters
- Requires digit
- Requires uppercase
- Requires lowercase
- No special characters required

### Invitation Code
- 6-digit numeric code
- Expires in 24 hours
- Single use only
- Sent via SMS (implement SMS service)

### JWT Token
- Expires in 24 hours (configurable)
- Contains user ID and roles
- Signed with secret key
- Validated on each request

### Best Practices
1. **Never return invitation code in production** - Only for testing
2. **Use HTTPS in production** - Protect token transmission
3. **Store tokens securely** - Use httpOnly cookies or secure storage
4. **Implement token refresh** - For long-lived sessions
5. **Log authentication events** - For security auditing

## Error Handling

### Common Errors

1. **Registration Errors**:
   - "Mobile number already exists"
   - "National number already exists"
   - "Failed to create user" (password validation failed)

2. **Activation Errors**:
   - "Customer not found"
   - "Customer is already activated"
   - "Invalid or expired invitation code"

3. **Login Errors**:
   - "Invalid mobile number or password"
   - "User is not a customer/admin"
   - "Customer account is not activated"

## Code Locations Summary

### Commands
- Customer Register: `Application/Features/Customer/Command/RegisterCustomerCommand/`
- Customer Activate: `Application/Features/Customer/Command/ActivateCustomerCommand/`
- Customer Login: `Application/Features/Customer/Command/CustomerLoginCommand/`
- Admin Login: `Application/Features/Admin/Command/AdminLoginCommand/`

### Controllers
- Customer Controller: `ECommerce.Server/Controllers/CustomerController.cs`
- Admin Controller: `ECommerce.Server/Controllers/AdminController.cs`

### Services
- JWT Token Service: `Infrastructure/Services/JwtTokenService.cs`
- Invitation Code Service: `Infrastructure/Services/InvitationCodeService.cs`
- User Session: `Infrastructure/Services/UserSession.cs`

### Configuration
- JWT Configuration: `Infrastructure/Configuration/JwtConfiguration.cs`
- Database Configuration: `Infrastructure/DatabaseConfiguration.cs`
- Seed Data: `Infrastructure/Data/SeedData.cs`

### Entities
- Customer: `Domain/Models/Customer.cs`
- ApplicationUser: `Domain/Models/ApplicationUser.cs`
- ApplicationRole: `Domain/Models/ApplicationRole.cs`

## Testing the Flow

### 1. Test Customer Registration
```http
POST /api/Customer/Register
Content-Type: application/json

{
  "mobileNumber": "01234567890",
  "userName": "Test Customer",
  "nationalNumber": "12345678901234",
  "gender": "Male",
  "password": "Test@12345"
}
```

### 2. Test Customer Activation
```http
POST /api/Customer/Activate
Content-Type: application/json

{
  "mobileNumber": "01234567890",
  "invitationCode": "123456"  // Use code from registration response
}
```

### 3. Test Customer Login
```http
POST /api/Customer/Login
Content-Type: application/json

{
  "mobileNumber": "01234567890",
  "password": "Test@12345"
}
```

### 4. Test Admin Login
```http
POST /api/Admin/Login
Content-Type: application/json

{
  "userName": "admin",
  "password": "Admin@123"
}
```

### 5. Use Token in Authenticated Requests
```http
GET /api/SomeProtectedEndpoint
Authorization: Bearer {token_from_login_response}
```

## Next Steps

1. **Implement SMS Service**: Replace console log with actual SMS provider
2. **Add Token Refresh**: Implement refresh token mechanism
3. **Add Logout**: Implement token blacklisting
4. **Add Password Reset**: Implement password reset flow
5. **Add Profile Management**: Allow customers to update their profile

