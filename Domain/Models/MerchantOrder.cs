using Domain.Common;
using Domain.Enums;

namespace Domain.Models
{
    public class MerchantOrder : IAuditable
    {
        public int MerchantOrderId { get; private set; }
        public int OrderId { get; private set; }
        public int MerchantId { get; private set; }
        public MerchantOrderResponseStatus ResponseStatus { get; private set; }
        public string? RejectReason { get; private set; }
        public DateTime? RespondedAt { get; private set; }

        public Order Order { get; private set; } = null!;
        public Merchant Merchant { get; private set; } = null!;

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        private MerchantOrder() { }

        public static MerchantOrder Create(int orderId, int merchantId, string? createdBy = null)
        {
            if (orderId <= 0)
                throw new ArgumentException("Order ID must be greater than zero", nameof(orderId));
            if (merchantId <= 0)
                throw new ArgumentException("Merchant ID must be greater than zero", nameof(merchantId));

            return new MerchantOrder
            {
                OrderId = orderId,
                MerchantId = merchantId,
                ResponseStatus = MerchantOrderResponseStatus.Pending,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        public void Accept(string? modifiedBy = null)
        {
            if (ResponseStatus == MerchantOrderResponseStatus.Accepted)
                return;

            if (ResponseStatus == MerchantOrderResponseStatus.Rejected)
                throw new InvalidOperationException("Cannot accept a rejected merchant order invitation.");

            ResponseStatus = MerchantOrderResponseStatus.Accepted;
            RejectReason = null;
            RespondedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void Reject(string reason, string? modifiedBy = null)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reject reason is required", nameof(reason));

            if (ResponseStatus == MerchantOrderResponseStatus.Accepted)
                throw new InvalidOperationException("Cannot reject an already accepted merchant order invitation.");

            ResponseStatus = MerchantOrderResponseStatus.Rejected;
            RejectReason = reason.Trim();
            RespondedAt = DateTime.UtcNow;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        public void ResetToPending(string? modifiedBy = null)
        {
            ResponseStatus = MerchantOrderResponseStatus.Pending;
            RejectReason = null;
            RespondedAt = null;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}
