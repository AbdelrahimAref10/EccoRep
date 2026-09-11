using Domain.Common;
using Domain.Enums;

namespace Domain.Models
{
    /// <summary>
    /// Append-only debit/credit ledger. OrderId is set for order movements;
    /// null for party-level floats (e.g. delivery cash float).
    /// </summary>
    public class OrderJournal : IAuditable
    {
        public int OrderJournalId { get; private set; }
        public int? OrderId { get; private set; }
        public LedgerPartyType PartyType { get; private set; }
        public int? PartyId { get; private set; }
        public JournalDirection Direction { get; private set; }
        public decimal Amount { get; private set; }
        public OrderJournalEntryKind EntryKind { get; private set; }
        public string IdempotencyKey { get; private set; } = string.Empty;
        public FaultParty? FaultParty { get; private set; }
        public string? Note { get; private set; }

        public Order? Order { get; private set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        private OrderJournal() { }

        public static OrderJournal Create(
            int? orderId,
            LedgerPartyType partyType,
            int? partyId,
            JournalDirection direction,
            decimal amount,
            OrderJournalEntryKind entryKind,
            string idempotencyKey,
            string? note = null,
            FaultParty? faultParty = null,
            string? createdBy = null)
        {
            if (orderId.HasValue && orderId.Value <= 0)
                throw new ArgumentException("Order ID must be greater than zero when provided", nameof(orderId));
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero", nameof(amount));
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                throw new ArgumentException("Idempotency key is required", nameof(idempotencyKey));
            if (partyType != LedgerPartyType.Company && (!partyId.HasValue || partyId.Value <= 0))
                throw new ArgumentException("Party ID is required for merchant/delivery entries", nameof(partyId));

            var requiresOrder = entryKind is not (
                OrderJournalEntryKind.DeliveryCashFloatReceived or
                OrderJournalEntryKind.DeliveryCashFloatReturned);

            if (requiresOrder && !orderId.HasValue)
                throw new ArgumentException($"Entry kind {entryKind} requires an OrderId", nameof(orderId));

            if (!requiresOrder && orderId.HasValue)
                throw new ArgumentException($"Entry kind {entryKind} must not have an OrderId", nameof(orderId));

            return new OrderJournal
            {
                OrderId = orderId,
                PartyType = partyType,
                PartyId = partyType == LedgerPartyType.Company ? null : partyId,
                Direction = direction,
                Amount = amount,
                EntryKind = entryKind,
                IdempotencyKey = idempotencyKey.Trim(),
                Note = note,
                FaultParty = faultParty,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }
    }
}
