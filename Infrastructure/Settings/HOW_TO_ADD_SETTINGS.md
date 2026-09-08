# How to Add New Settings from appsettings.json

This guide shows you the **standard pattern** for adding any new configuration settings from `appsettings.json`.

## Standard Pattern (3 Steps)

### Step 1: Create Interface and Class

Create two files in `Infrastructure/Settings/`:

**Example: Adding SMS Settings**

1. **Create Interface** (`ISmsSettings.cs`):
```csharp
namespace Infrastructure.Settings
{
    public interface ISmsSettings
    {
        string ApiKey { get; }
        string SenderId { get; }
        string BaseUrl { get; }
        int TimeoutSeconds { get; }
    }
}
```

2. **Create Class** (`SmsSettings.cs`):
```csharp
namespace Infrastructure.Settings
{
    public class SmsSettings : ISmsSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
    }
}
```

### Step 2: Add to appsettings.json

```json
{
  "Sms": {
    "ApiKey": "your-api-key-here",
    "SenderId": "YourApp",
    "BaseUrl": "https://api.smsprovider.com",
    "TimeoutSeconds": 30
  }
}
```

### Step 3: Register in DatabaseConfiguration.cs

Add registration in `Infrastructure/DatabaseConfiguration.cs`:

```csharp
// Register strongly-typed configuration settings
services.Configure<SmsSettings>(configuration.GetSection("Sms"));

// Register as singleton for direct injection
services.AddSingleton<ISmsSettings>(sp =>
{
    var smsSettings = new SmsSettings();
    configuration.GetSection("Sms").Bind(smsSettings);
    return smsSettings;
});
```

## Usage Examples

### Option 1: Direct Injection (Recommended)

```csharp
public class SmsService
{
    private readonly ISmsSettings _smsSettings;

    public SmsService(ISmsSettings smsSettings)
    {
        _smsSettings = smsSettings;
    }

    public void SendSms()
    {
        var apiKey = _smsSettings.ApiKey;
        var senderId = _smsSettings.SenderId;
        // Use settings...
    }
}
```

### Option 2: Using IOptions<T>

```csharp
using Microsoft.Extensions.Options;

public class SmsService
{
    private readonly SmsSettings _smsSettings;

    public SmsService(IOptions<SmsSettings> smsSettings)
    {
        _smsSettings = smsSettings.Value;
    }

    public void SendSms()
    {
        var apiKey = _smsSettings.ApiKey;
        // Use settings...
    }
}
```

## Complete Example: Adding Email Settings

### 1. Create Files

**`Infrastructure/Settings/IEmailSettings.cs`:**
```csharp
namespace Infrastructure.Settings
{
    public interface IEmailSettings
    {
        string SmtpServer { get; }
        int SmtpPort { get; }
        string Username { get; }
        string Password { get; }
        bool EnableSsl { get; }
    }
}
```

**`Infrastructure/Settings/EmailSettings.cs`:**
```csharp
namespace Infrastructure.Settings
{
    public class EmailSettings : IEmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }
}
```

### 2. Add to appsettings.json

```json
{
  "Email": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-password",
    "EnableSsl": true
  }
}
```

### 3. Register in DatabaseConfiguration.cs

```csharp
// Add this in AddDatabaseServices method
services.Configure<EmailSettings>(configuration.GetSection("Email"));

services.AddSingleton<IEmailSettings>(sp =>
{
    var emailSettings = new EmailSettings();
    configuration.GetSection("Email").Bind(emailSettings);
    return emailSettings;
});
```

### 4. Use in Service

```csharp
public class EmailService
{
    private readonly IEmailSettings _emailSettings;

    public EmailService(IEmailSettings emailSettings)
    {
        _emailSettings = emailSettings;
    }

    public void SendEmail()
    {
        var server = _emailSettings.SmtpServer;
        var port = _emailSettings.SmtpPort;
        // Use settings...
    }
}
```

## Pattern Summary

For **any new settings**, follow this pattern:

1. ✅ Create `I[Name]Settings.cs` interface
2. ✅ Create `[Name]Settings.cs` class implementing the interface
3. ✅ Add section to `appsettings.json`
4. ✅ Register in `DatabaseConfiguration.cs`:
   - `services.Configure<[Name]Settings>(configuration.GetSection("[Name]"));`
   - `services.AddSingleton<I[Name]Settings>(...)`
5. ✅ Inject `I[Name]Settings` in your services

## Benefits

- ✅ **Type Safety**: Compile-time checking
- ✅ **IntelliSense**: Auto-completion in IDE
- ✅ **Testability**: Easy to mock interfaces
- ✅ **Consistency**: Same pattern for all settings
- ✅ **No Magic Strings**: No `configuration["Section:Key"]` scattered in code

