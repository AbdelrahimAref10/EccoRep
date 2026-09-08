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
    }
}

