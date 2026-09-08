using Domain.Common;
using Domain.Models;

namespace Domain.Services
{
    public class OrderCalculationService
    {
        /// <summary>
        /// Calculates the order rental subtotal (price × vehicles × inclusive reservation days)
        /// </summary>
        public static decimal CalculateOrderSubTotal(decimal subCategoryPrice, int vehiclesCount, int reservationDays)
        {
            if (subCategoryPrice < 0)
                throw new ArgumentException("SubCategory price cannot be negative", nameof(subCategoryPrice));

            if (vehiclesCount <= 0)
                throw new ArgumentException("Vehicles count must be greater than zero", nameof(vehiclesCount));

            if (reservationDays <= 0)
                throw new ArgumentException("Reservation days must be greater than zero", nameof(reservationDays));

            return subCategoryPrice * vehiclesCount * reservationDays;
        }

        /// <summary>
        /// Calculates the full order total with all fees.
        /// SubTotal must already include reservation days (price × vehicles × days).
        /// Formula: SubTotal + (DeliveryFees * Count) + ServiceFees + UrgentDelivery (if urgent) - (TieredDiscount % * SubTotal)
        /// </summary>
        public static decimal CalculateOrderTotal(
            decimal subTotal,
            decimal? deliveryFees,
            decimal? serviceFees,
            decimal? urgentDelivery,
            decimal tieredDiscountPercentage,
            int vehiclesCount,
            bool isUrgent)
        {
            if (subTotal < 0)
                throw new ArgumentException("SubTotal cannot be negative", nameof(subTotal));

            if (vehiclesCount <= 0)
                throw new ArgumentException("Vehicles count must be greater than zero", nameof(vehiclesCount));

            if (tieredDiscountPercentage < 0 || tieredDiscountPercentage > 100)
                throw new ArgumentException("Tiered discount percentage must be between 0 and 100", nameof(tieredDiscountPercentage));

            decimal total = subTotal;

            // Add delivery fees (per vehicle - amount)
            if (deliveryFees.HasValue && deliveryFees.Value > 0)
            {
                total += deliveryFees.Value * vehiclesCount;
            }

            // Add service fees (amount)
            if (serviceFees.HasValue && serviceFees.Value > 0)
            {
                total += serviceFees.Value;
            }

            // Add urgent delivery fee (amount, if urgent)
            if (isUrgent && urgentDelivery.HasValue && urgentDelivery.Value > 0)
            {
                total += urgentDelivery.Value;
            }

            // Apply tiered discount (percentage of subtotal only, not fees)
            if (tieredDiscountPercentage > 0)
            {
                var tieredDiscountAmount = tieredDiscountPercentage * subTotal / 100;
                total -= tieredDiscountAmount;
            }

            return total;
        }

        /// <summary>
        /// Calculates the tiered discount amount based on subtotal and discount percentage
        /// </summary>
        public static decimal CalculateTieredDiscountAmount(decimal subTotal, decimal tieredDiscountPercentage)
        {
            if (subTotal < 0)
                throw new ArgumentException("SubTotal cannot be negative", nameof(subTotal));

            if (tieredDiscountPercentage < 0 || tieredDiscountPercentage > 100)
                throw new ArgumentException("Tiered discount percentage must be between 0 and 100", nameof(tieredDiscountPercentage));

            if (tieredDiscountPercentage == 0)
                return 0;

            return tieredDiscountPercentage * subTotal / 100;
        }

        /// <summary>
        /// Validates that the backend total matches the mobile total within tolerance
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

