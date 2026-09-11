namespace Domain.Enums
{
    /// <summary>Admin "ادفع للدليفري" payment kinds.</summary>
    public enum AdminPayDeliveryKind
    {
        /// <summary>Cash float / عُهدة at start of day (no order).</summary>
        CashFloat = 1,
        /// <summary>Settle open credit on an order (fee + advances).</summary>
        OrderPayout = 2
    }

    /// <summary>Admin "اقبض من الدليفري" collection kinds.</summary>
    public enum AdminCollectFromDeliveryKind
    {
        /// <summary>Customer cash remittance for an order.</summary>
        OrderCashRemittance = 1,
        /// <summary>Return unused cash float (no order).</summary>
        FloatReturn = 2
    }
}
