namespace Domain.Enums
{
    /// <summary>
    /// Remapped values (existing DB rows need a data migration):
    /// Old: Confirmed=1, OnWay=2, CustomerReceived=3, Completed=4, Cancelled=5
    /// New: insert Merchant* + DeliveryAssigned; shift later values.
    /// </summary>
    public enum OrderState
    {
        Pending = 0,
        MerchantPending = 1,
        MerchantConfirmed = 2,
        Confirmed = 3,
        /// <summary>Deliveries assigned per vehicle after Confirmed.</summary>
        DeliveryAssigned = 4,
        OnWay = 5,
        CustomerReceived = 6,
        CustomerRejectedReceipt = 7,
        Completed = 8,
        Cancelled = 9
    }
}
