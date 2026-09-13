using Domain.Enums;

namespace Domain.Models
{
    /// <summary>Immutable journal instruction produced by the Order aggregate (no calculation in commands).</summary>
    public sealed class OrderLedgerLine
    {
        public int? OrderId { get; }
        public int? VehicleId { get; }
        public LedgerPartyType PartyType { get; }
        public int? PartyId { get; }
        public JournalDirection Direction { get; }
        public decimal Amount { get; }
        public OrderJournalEntryKind EntryKind { get; }
        public string IdempotencyKey { get; }
        public string? Note { get; }
        public FaultParty? FaultParty { get; }

        public OrderLedgerLine(
            int? orderId,
            int? vehicleId,
            LedgerPartyType partyType,
            int? partyId,
            JournalDirection direction,
            decimal amount,
            OrderJournalEntryKind entryKind,
            string idempotencyKey,
            string? note = null,
            FaultParty? faultParty = null)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero", nameof(amount));
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                throw new ArgumentException("Idempotency key is required", nameof(idempotencyKey));

            OrderId = orderId;
            VehicleId = vehicleId;
            PartyType = partyType;
            PartyId = partyType == LedgerPartyType.Company ? null : partyId;
            Direction = direction;
            Amount = amount;
            EntryKind = entryKind;
            IdempotencyKey = idempotencyKey.Trim();
            Note = note;
            FaultParty = faultParty;
        }
    }
}
