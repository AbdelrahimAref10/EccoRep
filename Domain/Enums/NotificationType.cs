namespace Domain.Enums
{
    public enum NotificationType
    {
        OrderCreated = 1,
        OrderConfirmed = 2,
        OrderOnWay = 3,
        OrderCustomerReceived = 4,
        OrderCompleted = 5,
        OrderCancelled = 6,
        /// <summary>Admin sent order to merchant(s) — MerchantPending invitation.</summary>
        OrderMerchantPending = 7,
        /// <summary>Generic order-cycle change (replace vehicle, handover, delivery assignment, merchant response, etc.).</summary>
        OrderUpdated = 8
    }
}


