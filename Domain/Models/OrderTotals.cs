namespace Domain.Models
{
    public class OrderTotals
    {
        // Private setters for encapsulation
        public int Id { get; private set; }
        public int OrderId { get; private set; }
        public decimal SubTotal { get; private set; }
        public decimal ServiceFees { get; private set; }
        public decimal DeliveryFees { get; private set; }
        public decimal UrgentFees { get; private set; }
        public decimal TieredDiscount { get; private set; }
        public decimal TotalAfterAllFees { get; private set; }

        // Navigation property
        public Order Order { get; private set; } = null!;

        // Private constructor for EF Core
        private OrderTotals() { }

        // Factory method for creating order totals
        public static OrderTotals Create(
            int orderId,
            decimal subTotal,
            decimal serviceFees,
            decimal deliveryFees,
            decimal urgentFees,
            decimal tieredDiscount,
            decimal totalAfterAllFees)
        {
            if (orderId <= 0)
                throw new ArgumentException("Order ID must be greater than zero", nameof(orderId));

            if (subTotal < 0)
                throw new ArgumentException("SubTotal cannot be negative", nameof(subTotal));

            if (serviceFees < 0)
                throw new ArgumentException("Service fees cannot be negative", nameof(serviceFees));

            if (deliveryFees < 0)
                throw new ArgumentException("Delivery fees cannot be negative", nameof(deliveryFees));

            if (urgentFees < 0)
                throw new ArgumentException("Urgent fees cannot be negative", nameof(urgentFees));

            if (tieredDiscount < 0)
                throw new ArgumentException("Tiered discount cannot be negative", nameof(tieredDiscount));

            if (totalAfterAllFees < 0)
                throw new ArgumentException("Total after all fees cannot be negative", nameof(totalAfterAllFees));

            return new OrderTotals
            {
                OrderId = orderId,
                SubTotal = subTotal,
                ServiceFees = serviceFees,
                DeliveryFees = deliveryFees,
                UrgentFees = urgentFees,
                TieredDiscount = tieredDiscount,
                TotalAfterAllFees = totalAfterAllFees
            };
        }

        public void Update(
            decimal subTotal,
            decimal serviceFees,
            decimal deliveryFees,
            decimal urgentFees,
            decimal tieredDiscount,
            decimal totalAfterAllFees)
        {
            if (subTotal < 0)
                throw new ArgumentException("SubTotal cannot be negative", nameof(subTotal));

            if (serviceFees < 0)
                throw new ArgumentException("Service fees cannot be negative", nameof(serviceFees));

            if (deliveryFees < 0)
                throw new ArgumentException("Delivery fees cannot be negative", nameof(deliveryFees));

            if (urgentFees < 0)
                throw new ArgumentException("Urgent fees cannot be negative", nameof(urgentFees));

            if (tieredDiscount < 0)
                throw new ArgumentException("Tiered discount cannot be negative", nameof(tieredDiscount));

            if (totalAfterAllFees < 0)
                throw new ArgumentException("Total after all fees cannot be negative", nameof(totalAfterAllFees));

            SubTotal = subTotal;
            ServiceFees = serviceFees;
            DeliveryFees = deliveryFees;
            UrgentFees = urgentFees;
            TieredDiscount = tieredDiscount;
            TotalAfterAllFees = totalAfterAllFees;
        }
    }
}
