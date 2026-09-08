namespace Application.Features.Order.DTOs
{
    public class OrderVehicleDto
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string VehicleCode { get; set; } = string.Empty;
        /// <summary>VehicleStatus as int: Available=0, UnderMaintenance=1, Rented=2.</summary>
        public int Status { get; set; }
    }
}
