using Domain.Common;

namespace Infrastructure.Services
{
    /// <summary>
    /// Provides date and time operations for Egypt timezone
    /// </summary>
    public class DateTimeProvider : IDateTimeProvider
    {
        public const string TimeZone = "Egypt Standard Time";

        public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                               TimeZoneInfo.FindSystemTimeZoneById(TimeZone));

        public DateTimeKind Kind => DateTimeKind.Local;

        public bool SupportsMultipleTimeZone => false;

        public DateTime Normalize(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Local);
            }

            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(dateTime,
                    TimeZoneInfo.FindSystemTimeZoneById(TimeZone));
            }

            return dateTime;
        }
    }
}
