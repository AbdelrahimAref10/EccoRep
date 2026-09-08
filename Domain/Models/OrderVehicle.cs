using Domain.Common;

namespace Domain.Models
{
    public class OrderVehicle : IAuditable
    {
        // Composite primary key
        public int OrderId { get; private set; }
        public int VehicleId { get; private set; }

        // Navigation properties
        public Order Order { get; private set; } = null!;
        public Vehicle Vehicle { get; private set; } = null!;

        // Audit properties
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        // Private constructor for EF Core
        private OrderVehicle() { }

        // Factory method for creating order-vehicle relationships
        public static OrderVehicle Create(
            int orderId,
            int vehicleId,
            string? createdBy = null)
        {
            if (orderId <= 0)
                throw new ArgumentException("Order ID must be greater than zero", nameof(orderId));

            if (vehicleId <= 0)
                throw new ArgumentException("Vehicle ID must be greater than zero", nameof(vehicleId));

            return new OrderVehicle
            {
                OrderId = orderId,
                VehicleId = vehicleId,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }
    }
}

