# Configuration Settings

This folder contains strongly-typed configuration interfaces and classes for accessing `appsettings.json` values.

## Available Settings

### IJwtSettings / JwtSettings
Interface and class for JWT configuration settings.

**Properties:**
- `Key` (string): Secret key for signing JWT tokens
- `Issuer` (string): Token issuer
- `Audience` (string): Token audience
- `ExpirationHours` (int): Token expiration time in hours (default: 24)

**appsettings.json:**
```json
{
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLongForHS256Algorithm",
    "Issuer": "VoltApi",
    "Audience": "VoltClient",
    "ExpirationHours": "24"
  }
}
```

## Usage Example

### Injecting IJwtSettings

```csharp
public class MyService
{
    private readonly IJwtSettings _jwtSettings;

    public MyService(IJwtSettings jwtSettings)
    {
        _jwtSettings = jwtSettings;
    }

    public void DoSomething()
    {
        var key = _jwtSettings.Key;
        var issuer = _jwtSettings.Issuer;
        var expiration = _jwtSettings.ExpirationHours;
    }
}
```

### Using IOptions<T> Pattern (Alternative)

You can also use the `IOptions<T>` pattern:

```csharp
using Microsoft.Extensions.Options;

public class MyService
{
    private readonly IOptions<JwtSettings> _jwtSettings;

    public MyService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings;
    }

    public void DoSomething()
    {
        var key = _jwtSettings.Value.Key;
    }
}
```

## Adding New Settings

See **HOW_TO_ADD_SETTINGS.md** for the standard pattern to add any new configuration settings.

## Registration

Settings are automatically registered in `Infrastructure/DatabaseConfiguration.cs` during service configuration.

## Benefits

1. **Type Safety**: Compile-time checking instead of magic strings
2. **IntelliSense**: Auto-completion in IDE
3. **Refactoring**: Easy to rename properties
4. **Testability**: Easy to mock interfaces in unit tests
5. **Validation**: Can add validation attributes to properties

