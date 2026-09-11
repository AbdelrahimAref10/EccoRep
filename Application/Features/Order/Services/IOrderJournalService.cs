using CSharpFunctionalExtensions;
using Domain.Enums;
using Domain.Models;

namespace Application.Features.Order.Services
{
    /// <summary>
    /// Single entry-point for all VO_OrderJournal posts (merchant, delivery, company/Ecco).
    /// Commands must not insert OrderJournal rows directly — use this service.
    /// </summary>
    public interface IOrderJournalService
    {
        /// <summary>
        /// Idempotent post. If <paramref name="idempotencyKey"/> already exists, returns success without duplicating.
        /// Pass <paramref name="orderId"/> null only for float entry kinds.
        /// </summary>
        Task<Result> PostAsync(
            int? orderId,
            LedgerPartyType partyType,
            int? partyId,
            JournalDirection direction,
            decimal amount,
            OrderJournalEntryKind entryKind,
            string idempotencyKey,
            string? note = null,
            FaultParty? faultParty = null,
            string? createdBy = null,
            CancellationToken cancellationToken = default);

        Task<Result> PostCreditAsync(
            int? orderId,
            LedgerPartyType partyType,
            int? partyId,
            decimal amount,
            OrderJournalEntryKind entryKind,
            string idempotencyKey,
            string? note = null,
            FaultParty? faultParty = null,
            string? createdBy = null,
            CancellationToken cancellationToken = default);

        Task<Result> PostDebitAsync(
            int? orderId,
            LedgerPartyType partyType,
            int? partyId,
            decimal amount,
            OrderJournalEntryKind entryKind,
            string idempotencyKey,
            string? note = null,
            FaultParty? faultParty = null,
            string? createdBy = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Balance for a party on one order (order-scoped rows only).
        /// Balance = Σ Credit − Σ Debit (positive = ليه).
        /// </summary>
        Task<decimal> GetPartyBalanceAsync(
            int orderId,
            LedgerPartyType partyType,
            int? partyId,
            CancellationToken cancellationToken = default);

        /// <summary>Global party balance across all orders + float rows.</summary>
        Task<decimal> GetGlobalPartyBalanceAsync(
            LedgerPartyType partyType,
            int partyId,
            CancellationToken cancellationToken = default);

        Task<decimal> GetOpenAmountAsync(
            int orderId,
            LedgerPartyType partyType,
            int? partyId,
            OrderJournalEntryKind accruedKind,
            OrderJournalEntryKind settledKind,
            JournalDirection accruedDirection,
            JournalDirection settledDirection,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<OrderJournal>> GetOrderEntriesAsync(
            int orderId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<OrderJournal>> GetPartyEntriesAsync(
            LedgerPartyType partyType,
            int partyId,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    }

    public static class OrderJournalKeys
    {
        public static string Build(int orderId, string suffix) => $"order:{orderId}:{suffix}";

        public static string BuildParty(LedgerPartyType partyType, int partyId, string suffix) =>
            $"party:{(int)partyType}:{partyId}:{suffix}";
    }
}
