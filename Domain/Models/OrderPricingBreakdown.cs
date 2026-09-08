namespace Domain.Models
{
    /// <summary>
    /// Immutable pricing snapshot produced by <see cref="Order.CalculatePricing"/>.
    /// Used by preview/calculate APIs and by create/update order flows.
    /// </summary>
    public sealed class OrderPricingBreakdown
    {
        public decimal UnitPrice { get; }
        public int VehiclesCount { get; }
        public decimal SubTotal { get; }
        public decimal DeliveryFees { get; }
        public decimal ServiceFees { get; }
        public decimal UrgentFees { get; }
        public decimal TieredDiscountPercentage { get; }
        public decimal TieredDiscountAmount { get; }
        /// <summary>Rental total before previous debt (subtotal + fees - discount).</summary>
        public decimal RentalTotal { get; }
        /// <summary>Pending cancellation debt from CustomerWallet.</summary>
        public decimal PreviousDebt { get; }
        /// <summary>Final payable total = RentalTotal + PreviousDebt.</summary>
        public decimal Total { get; }

        public OrderPricingBreakdown(
            decimal unitPrice,
            int vehiclesCount,
            decimal subTotal,
            decimal deliveryFees,
            decimal serviceFees,
            decimal urgentFees,
            decimal tieredDiscountPercentage,
            decimal tieredDiscountAmount,
            decimal rentalTotal,
            decimal previousDebt,
            decimal total)
        {
            UnitPrice = unitPrice;
            VehiclesCount = vehiclesCount;
            SubTotal = subTotal;
            DeliveryFees = deliveryFees;
            ServiceFees = serviceFees;
            UrgentFees = urgentFees;
            TieredDiscountPercentage = tieredDiscountPercentage;
            TieredDiscountAmount = tieredDiscountAmount;
            RentalTotal = rentalTotal;
            PreviousDebt = previousDebt;
            Total = total;
        }
    }
}
