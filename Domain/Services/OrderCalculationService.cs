using Domain.Common;
using Domain.Models;

namespace Domain.Services
{
    public class OrderCalculationService
    {
        /// <summary>
        /// Validates that the backend total matches the mobile total within tolerance.
        /// Order money math lives on <see cref="Order.CalculatePricing"/>.
        /// </summary>
        public static bool ValidateTotalMatch(decimal backendTotal, decimal mobileTotal, decimal tolerance = 0.50m)
        {
            var difference = Math.Abs(backendTotal - mobileTotal);
            return difference <= tolerance;
        }

        /// <summary>
        /// Calculates cancellation fee based on city and order age (2 days policy)
        /// Cancellation fee is a percentage of the order total
        /// </summary>
        public static decimal? CalculateCancellationFee(City city, DateTime orderCreatedDate, decimal orderTotal, IDateTimeProvider dateTimeProvider)
        {
            if (city == null)
                throw new ArgumentNullException(nameof(city));

            if (orderTotal < 0)
                throw new ArgumentException("Order total cannot be negative", nameof(orderTotal));

            var daysSinceCreation = (dateTimeProvider.Now - orderCreatedDate).TotalDays;

            // If order created within 2 days, no cancellation fee
            if (daysSinceCreation <= 2)
            {
                return null;
            }

            // If order created more than 2 days ago, apply cancellation fee (percentage of order total)
            if (city.CancellationFees.HasValue && city.CancellationFees.Value > 0)
            {
                return city.CancellationFees.Value * orderTotal / 100;
            }

            return null;
        }
    }
}

