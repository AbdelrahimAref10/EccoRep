namespace Infrastructure.Settings
{
    /// <summary>
    /// JWT configuration settings from appsettings.json
    /// </summary>
    public class JwtSettings : IJwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpirationHours { get; set; } = 24;
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }
}

