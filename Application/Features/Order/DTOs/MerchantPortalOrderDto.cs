using Domain.Enums;

namespace Application.Features.Order.DTOs
{
    /// <summary>Merchant portal: one order row scoped to the logged-in merchant.</summary>
    public class MerchantPortalOrderListItemDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string SubCategoryName { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public DateTime ReservationDateFrom { get; set; }
        public DateTime ReservationDateTo { get; set; }
        public OrderState OrderState { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsUrgent { get; set; }

        /// <summary>This merchant's invitation response on the order.</summary>
        public MerchantOrderResponseStatus MyResponseStatus { get; set; }
        public string? MyRejectReason { get; set; }
        public DateTime? MyRespondedAt { get; set; }

        /// <summary>Vehicles on this order that belong to this merchant.</summary>
        public int MyVehiclesCount { get; set; }
        public decimal MyNetTotal { get; set; }
        public decimal MyRentalTotal { get; set; }
        public decimal MyServiceFeeTotal { get; set; }

        public bool CanAccept { get; set; }
        public bool CanReject { get; set; }
        public int PendingHandoverVehicleCount { get; set; }
        public bool CanHandover { get; set; }
    }

    /// <summary>Merchant portal: order detail with only this merchant's slice of data.</summary>
    public class MerchantPortalOrderDetailDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string SubCategoryName { get; set; } = string.Empty;
        public decimal SubCategoryPrice { get; set; }
        public string CityName { get; set; } = string.Empty;
        public DateTime ReservationDateFrom { get; set; }
        public DateTime ReservationDateTo { get; set; }
        public int VehiclesCount { get; set; }
        public bool IsUrgent { get; set; }
        public OrderState OrderState { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? Notes { get; set; }

        public string HotelName { get; set; } = string.Empty;
        public string HotelAddress { get; set; } = string.Empty;
        public string? HotelPhone { get; set; }

        public bool CashOnReceive { get; set; }

        public MerchantOrderResponseStatus MyResponseStatus { get; set; }
        public string? MyRejectReason { get; set; }
        public DateTime? MyRespondedAt { get; set; }

        public bool CanAccept { get; set; }
        public bool CanReject { get; set; }
        public bool CanHandover { get; set; }

        public List<MerchantPortalVehicleDto> MyVehicles { get; set; } = new();
        public List<MerchantOrderPaymentDetailDto> MyPaymentDetails { get; set; } = new();
        public List<MerchantPortalHandoverDto> MyHandovers { get; set; } = new();
        public List<OrderJournalDto> MyJournals { get; set; } = new();

        public decimal MyRentalTotal { get; set; }
        public decimal MyServiceFeeTotal { get; set; }
        public decimal MyNetTotal { get; set; }
    }

    public class MerchantPortalVehicleDto
    {
        public int VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string VehicleCode { get; set; } = string.Empty;
        public int Status { get; set; }
    }

    public class MerchantPortalHandoverDto
    {
        public int VehicleId { get; set; }
        public string VehicleCode { get; set; } = string.Empty;
        public int? DeliveryId { get; set; }
        public string? DeliveryName { get; set; }
        public bool DeliveryReceivedFromMerchant { get; set; }
        public DateTime? ReceivedFromMerchantAt { get; set; }
        public bool AssignedToDelivery { get; set; }
    }

    public class MerchantDashboardSummaryDto
    {
        public int MerchantId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public bool CashOnReceive { get; set; }
        public int PendingInvitationsCount { get; set; }
        public int AwaitingHandoverCount { get; set; }
        public int ActiveOrdersCount { get; set; }
        public int MyVehiclesCount { get; set; }
        public decimal Balance { get; set; }
        public decimal AmountOwedToCompany { get; set; }
        public decimal AmountOwedByCompany { get; set; }
    }
}
