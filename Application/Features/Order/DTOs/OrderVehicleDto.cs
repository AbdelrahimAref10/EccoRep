using Domain.Enums;

namespace Application.Features.Order.DTOs
{
    public class OrderVehicleDto
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string VehicleCode { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int MerchantId { get; set; }
        public string MerchantName { get; set; } = string.Empty;
        /// <summary>VehicleStatus as int: Available=0, UnderMaintenance=1, Rented=2.</summary>
        public int Status { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal DeliveryFee { get; set; }
        /// <summary>Top speed in km/h. Optional.</summary>
        public int? SpeedKmh { get; set; }
        /// <summary>Motor engine capacity in CC. Optional.</summary>
        public int? EngineCapacityCc { get; set; }

        public bool ReceivedFromOwner { get; set; }
        public string? ReceivedFromOwnerImageUrl { get; set; }
        public bool DeliveredToCustomer { get; set; }
        public string? DeliveredToCustomerImageUrl { get; set; }
        public bool ReceivedFromCustomer { get; set; }
        public string? ReceivedFromCustomerImageUrl { get; set; }
        public bool DeliveredToOwner { get; set; }
        public string? DeliveredToOwnerImageUrl { get; set; }
        public bool DeliveryFailed { get; set; }
        public string? DeliveryFailureReason { get; set; }
        public FaultParty? DeliveryFailureFaultParty { get; set; }
        public MerchantVehicleResponseStatus MerchantResponseStatus { get; set; }
    }
}
