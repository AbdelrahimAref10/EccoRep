namespace Domain.Enums
{
    public enum OrderJournalEntryKind
    {
        MerchantRentalAccrued = 1,
        MerchantPaidByDeliveryCashOnReceive = 2,
        DeliveryCashAdvanceToMerchant = 3,
        DeliveryFeeAccrued = 4,
        CashCollectedFromCustomer = 5,
        CompanyServiceFeeAccrued = 6,
        DeliveryRemittanceToCompany = 7,
        MerchantPaidByCompany = 8,
        DeliveryPaidByCompany = 9,
        FaultClawback = 10,
        CustomerRejectedReceiptNote = 11,
        /// <summary>Delivery received cash float from company (عليه) — OrderId null.</summary>
        DeliveryCashFloatReceived = 12,
        /// <summary>Delivery returned unused float to company (ليه/تصفير) — OrderId null.</summary>
        DeliveryCashFloatReturned = 13,
        /// <summary>Company debit for full order total (cash first customer delivery, or PayPal capture).</summary>
        OrderTotalDebitedToCompany = 14,
        /// <summary>Fault party debit after non-delivery (vehicle or whole order).</summary>
        NonDeliveryFaultDebit = 15
    }
}
