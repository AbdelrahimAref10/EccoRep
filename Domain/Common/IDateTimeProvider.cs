namespace Domain.Common
{
    /// <summary>
    /// Provides date and time operations with timezone support
    /// </summary>
    public interface IDateTimeProvider
    {
        /// <summary>
        /// Gets the current date and time in the configured timezone
        /// </summary>
        DateTime Now { get; }

        /// <summary>
        /// Gets the kind of DateTime returned
        /// </summary>
        DateTimeKind Kind { get; }

        /// <summary>
        /// Indicates whether the provider supports multiple timezones
        /// </summary>
        bool SupportsMultipleTimeZone { get; }

        /// <summary>
        /// Normalizes a DateTime to the configured timezone
        /// </summary>
        /// <param name="dateTime">DateTime to normalize</param>
        /// <returns>Normalized DateTime</returns>
        DateTime Normalize(DateTime dateTime);
    }
}

