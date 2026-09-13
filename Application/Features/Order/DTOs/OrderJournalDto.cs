using Domain.Enums;

namespace Application.Features.Order.DTOs
{
    public class OrderJournalDto
    {
        public int OrderJournalId { get; set; }
        public int? OrderId { get; set; }
        public int? VehicleId { get; set; }
        public LedgerPartyType PartyType { get; set; }
        public int? PartyId { get; set; }
        public JournalDirection Direction { get; set; }
        public decimal Amount { get; set; }
        public OrderJournalEntryKind EntryKind { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
        public FaultParty? FaultParty { get; set; }
        public string? Note { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class PartyLedgerDto
    {
        public LedgerPartyType PartyType { get; set; }
        public int PartyId { get; set; }
        public string PartyName { get; set; } = string.Empty;
        /// <summary>ΣCredit − ΣDebit. Positive = ليه عند إيكو. Negative = عليه لإيكو.</summary>
        public decimal Balance { get; set; }
        public decimal AmountOwedToCompany { get; set; }
        public decimal AmountOwedByCompany { get; set; }
        public List<OrderJournalDto> Entries { get; set; } = new();
    }

    public class SettlementResultDto
    {
        public bool Success { get; set; }
        public decimal PostedAmount { get; set; }
        public decimal PartyBalanceAfter { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>Single journal row for admin movements list (includes party display name).</summary>
    public class OrderJournalMovementDto
    {
        public int OrderJournalId { get; set; }
        public int? OrderId { get; set; }
        public string? OrderCode { get; set; }
        public LedgerPartyType PartyType { get; set; }
        public int? PartyId { get; set; }
        public string? PartyName { get; set; }
        public JournalDirection Direction { get; set; }
        public decimal Amount { get; set; }
        public OrderJournalEntryKind EntryKind { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
        public FaultParty? FaultParty { get; set; }
        public string? Note { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// Admin movements list. Totals are over the filtered/visible entries only.
    /// Balance = TotalCredit − TotalDebit (positive = ليه, negative = عليه).
    /// </summary>
    public class OrderJournalListDto
    {
        public List<OrderJournalMovementDto> Entries { get; set; } = new();
        public decimal TotalCredit { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal Balance { get; set; }
    }
}
