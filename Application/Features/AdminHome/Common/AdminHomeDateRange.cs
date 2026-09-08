using System;

namespace Application.Features.AdminHome.Common
{
    public static class AdminHomeDateRange
    {
        /// <summary>
        /// Default: last 30 days inclusive (from start-of-day to end-of-day of "to").
        /// </summary>
        public static (DateTime From, DateTime To) Resolve(DateTime? from, DateTime? to, DateTime now)
        {
            var endDate = (to ?? now).Date;
            var startDate = (from ?? now.Date.AddDays(-29)).Date;

            if (startDate > endDate)
            {
                (startDate, endDate) = (endDate, startDate);
            }

            var fromUtc = startDate;
            var toUtc = endDate.AddDays(1).AddTicks(-1);
            return (fromUtc, toUtc);
        }

        public static (DateTime From, DateTime To) PreviousPeriod(DateTime from, DateTime to)
        {
            var length = to - from;
            var prevTo = from.AddTicks(-1);
            var prevFrom = prevTo - length;
            return (prevFrom, prevTo);
        }

        public static decimal PercentChange(decimal current, decimal previous)
        {
            if (previous == 0)
            {
                return current == 0 ? 0 : 100;
            }

            return Math.Round((current - previous) / previous * 100m, 2);
        }
    }
}
