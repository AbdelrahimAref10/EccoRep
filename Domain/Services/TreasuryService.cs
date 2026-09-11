using Domain.Models;

namespace Domain.Services
{
    public class TreasuryService
    {
        /// <summary>
        /// Creates a treasury record for cash payment when customer receives vehicle
        /// </summary>
        public static CompanyTreasury CreateCashPaymentRecord(
            decimal amount,
            string orderCode,
            string? createdBy = null)
        {
            return CompanyTreasury.Create(
                debitAmount: amount,
                creditAmount: 0,
                descriptionAr: $"دفع نقدي للطلب {orderCode}",
                descriptionEng: $"Cash payment for order {orderCode}",
                createdBy: createdBy);
        }

        /// <summary>
        /// Creates a treasury record for PayPal payment when payment is successful
        /// </summary>
        public static CompanyTreasury CreatePayPalPaymentRecord(
            decimal amount,
            string orderCode,
            string? createdBy = null)
        {
            return CompanyTreasury.Create(
                debitAmount: amount,
                creditAmount: 0,
                descriptionAr: $"دفع PayPal للطلب {orderCode}",
                descriptionEng: $"PayPal payment for order {orderCode}",
                createdBy: createdBy);
        }

        /// <summary>
        /// Creates a treasury record for cancellation fee
        /// </summary>
        public static CompanyTreasury CreateCancellationFeeRecord(
            decimal amount,
            string orderCode,
            string? createdBy = null)
        {
            return CompanyTreasury.Create(
                debitAmount: amount,
                creditAmount: 0,
                descriptionAr: $"رسوم إلغاء للطلب {orderCode}",
                descriptionEng: $"Cancellation fee for order {orderCode}",
                createdBy: createdBy);
        }

        public static CompanyTreasury CreateCancellationOrderRecord(
            decimal amount,
            string orderCode,
            string? createdBy = null)
        {
            return CompanyTreasury.Create(
                debitAmount: amount,
                creditAmount: 0,
                descriptionAr: $" إلغاء طلب {orderCode}",
                descriptionEng: $"Cancellation fee for order {orderCode}",
                createdBy: createdBy);
        }

        /// <summary>
        /// Creates a treasury debit when a delivery remits collected cash to the company.
        /// </summary>
        public static CompanyTreasury CreateDeliveryRemittanceRecord(
            decimal amount,
            string orderCode,
            int deliveryId,
            string? createdBy = null)
        {
            return CompanyTreasury.Create(
                debitAmount: amount,
                creditAmount: 0,
                descriptionAr: $"تحويل نقدي من المندوب {deliveryId} للطلب {orderCode}",
                descriptionEng: $"Delivery remittance from delivery {deliveryId} for order {orderCode}",
                createdBy: createdBy);
        }

        /// <summary>Company hands cash float to delivery (money leaves company vault conceptually as credit out / track separately).</summary>
        public static CompanyTreasury CreateDeliveryCashFloatOutRecord(
            decimal amount,
            int deliveryId,
            string? createdBy = null)
        {
            return CompanyTreasury.Create(
                debitAmount: 0,
                creditAmount: amount,
                descriptionAr: $"عُهدة كاش للمندوب {deliveryId}",
                descriptionEng: $"Cash float to delivery {deliveryId}",
                createdBy: createdBy);
        }

        public static CompanyTreasury CreateDeliveryCashFloatReturnRecord(
            decimal amount,
            int deliveryId,
            string? createdBy = null)
        {
            return CompanyTreasury.Create(
                debitAmount: amount,
                creditAmount: 0,
                descriptionAr: $"إرجاع عُهدة كاش من المندوب {deliveryId}",
                descriptionEng: $"Cash float returned from delivery {deliveryId}",
                createdBy: createdBy);
        }

        public static CompanyTreasury CreateMerchantPayoutRecord(
            decimal amount,
            string orderCode,
            int merchantId,
            string? createdBy = null)
        {
            return CompanyTreasury.Create(
                debitAmount: 0,
                creditAmount: amount,
                descriptionAr: $"سداد تاجر {merchantId} للطلب {orderCode}",
                descriptionEng: $"Merchant {merchantId} payout for order {orderCode}",
                createdBy: createdBy);
        }

        public static CompanyTreasury CreateDeliveryPayoutRecord(
            decimal amount,
            string orderCode,
            int deliveryId,
            string? createdBy = null)
        {
            return CompanyTreasury.Create(
                debitAmount: 0,
                creditAmount: amount,
                descriptionAr: $"سداد مندوب {deliveryId} للطلب {orderCode}",
                descriptionEng: $"Delivery {deliveryId} payout for order {orderCode}",
                createdBy: createdBy);
        }
    }
}

