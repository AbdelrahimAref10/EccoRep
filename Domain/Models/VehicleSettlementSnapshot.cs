namespace Domain.Models
{
    /// <summary>Per-vehicle settlement amounts loaded from payment detail snapshots (passed into Order methods).</summary>
    public sealed class VehicleSettlementSnapshot
    {
        public int VehicleId { get; init; }
        public int MerchantId { get; init; }
        public int DeliveryId { get; init; }
        public decimal VehicleRental { get; init; }
        public decimal OrderServiceFees { get; init; }
        public decimal DeliveryFeeShare { get; init; }
        public bool MerchantCashOnReceive { get; init; }
    }
}
