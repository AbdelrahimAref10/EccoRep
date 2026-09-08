namespace Infrastructure.Settings
{
    /// <summary>
    /// Interface for JWT configuration settings
    /// </summary>
    public interface IJwtSettings
    {
        string Key { get; }
        string Issuer { get; }
        string Audience { get; }
        int ExpirationHours { get; }
        int RefreshTokenExpirationDays { get; }
    }
}

