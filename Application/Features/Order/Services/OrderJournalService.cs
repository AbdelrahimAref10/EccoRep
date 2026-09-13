using CSharpFunctionalExtensions;
using Domain.Enums;
using Domain.Models;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Order.Services
{
    public class OrderJournalService : IOrderJournalService
    {
        private readonly DatabaseContext _context;

        public OrderJournalService(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return false;

            return await _context.OrderJournals
                .AnyAsync(j => j.IdempotencyKey == idempotencyKey.Trim(), cancellationToken);
        }

        public async Task<Result> PostAsync(
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
            int? vehicleId = null,
            CancellationToken cancellationToken = default)
        {
            if (amount <= 0)
                return Result.Failure("Journal amount must be greater than zero");

            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return Result.Failure("Idempotency key is required");

            if (partyType != LedgerPartyType.Company && (!partyId.HasValue || partyId.Value <= 0))
                return Result.Failure("Party ID is required for merchant and delivery journal entries");

            var key = idempotencyKey.Trim();
            if (await ExistsAsync(key, cancellationToken))
                return Result.Success();

            try
            {
                var entry = OrderJournal.Create(
                    orderId,
                    partyType,
                    partyId,
                    direction,
                    amount,
                    entryKind,
                    key,
                    note,
                    faultParty,
                    createdBy,
                    vehicleId);

                await _context.OrderJournals.AddAsync(entry, cancellationToken);
                return Result.Success();
            }
            catch (ArgumentException ex)
            {
                return Result.Failure(ex.Message);
            }
        }

        public Task<Result> PostCreditAsync(
            int? orderId,
            LedgerPartyType partyType,
            int? partyId,
            decimal amount,
            OrderJournalEntryKind entryKind,
            string idempotencyKey,
            string? note = null,
            FaultParty? faultParty = null,
            string? createdBy = null,
            int? vehicleId = null,
            CancellationToken cancellationToken = default)
        {
            return PostAsync(
                orderId, partyType, partyId, JournalDirection.Credit, amount, entryKind,
                idempotencyKey, note, faultParty, createdBy, vehicleId, cancellationToken);
        }

        public Task<Result> PostDebitAsync(
            int? orderId,
            LedgerPartyType partyType,
            int? partyId,
            decimal amount,
            OrderJournalEntryKind entryKind,
            string idempotencyKey,
            string? note = null,
            FaultParty? faultParty = null,
            string? createdBy = null,
            int? vehicleId = null,
            CancellationToken cancellationToken = default)
        {
            return PostAsync(
                orderId, partyType, partyId, JournalDirection.Debit, amount, entryKind,
                idempotencyKey, note, faultParty, createdBy, vehicleId, cancellationToken);
        }

        public async Task<Result> PostLinesAsync(
            IReadOnlyList<OrderLedgerLine> lines,
            string? createdBy = null,
            CancellationToken cancellationToken = default)
        {
            if (lines == null || lines.Count == 0)
                return Result.Success();

            foreach (var line in lines)
            {
                var result = await PostAsync(
                    line.OrderId,
                    line.PartyType,
                    line.PartyId,
                    line.Direction,
                    line.Amount,
                    line.EntryKind,
                    line.IdempotencyKey,
                    line.Note,
                    line.FaultParty,
                    createdBy,
                    line.VehicleId,
                    cancellationToken);

                if (result.IsFailure)
                    return result;
            }

            return Result.Success();
        }

        public async Task<decimal> GetPartyBalanceAsync(
            int orderId,
            LedgerPartyType partyType,
            int? partyId,
            CancellationToken cancellationToken = default)
        {
            var query = _context.OrderJournals.AsNoTracking()
                .Where(j => j.OrderId == orderId && j.PartyType == partyType);

            query = partyType == LedgerPartyType.Company
                ? query.Where(j => j.PartyId == null)
                : query.Where(j => j.PartyId == partyId);

            var credit = await query.Where(j => j.Direction == JournalDirection.Credit).SumAsync(j => j.Amount, cancellationToken);
            var debit = await query.Where(j => j.Direction == JournalDirection.Debit).SumAsync(j => j.Amount, cancellationToken);
            return credit - debit;
        }

        public async Task<decimal> GetGlobalPartyBalanceAsync(
            LedgerPartyType partyType,
            int partyId,
            CancellationToken cancellationToken = default)
        {
            var query = _context.OrderJournals.AsNoTracking()
                .Where(j => j.PartyType == partyType && j.PartyId == partyId);

            var credit = await query.Where(j => j.Direction == JournalDirection.Credit).SumAsync(j => j.Amount, cancellationToken);
            var debit = await query.Where(j => j.Direction == JournalDirection.Debit).SumAsync(j => j.Amount, cancellationToken);
            return credit - debit;
        }

        public async Task<decimal> GetOpenAmountAsync(
            int orderId,
            LedgerPartyType partyType,
            int? partyId,
            OrderJournalEntryKind accruedKind,
            OrderJournalEntryKind settledKind,
            JournalDirection accruedDirection,
            JournalDirection settledDirection,
            CancellationToken cancellationToken = default)
        {
            var query = _context.OrderJournals.AsNoTracking()
                .Where(j => j.OrderId == orderId && j.PartyType == partyType);

            query = partyType == LedgerPartyType.Company
                ? query.Where(j => j.PartyId == null)
                : query.Where(j => j.PartyId == partyId);

            var entries = await query
                .Where(j => j.EntryKind == accruedKind || j.EntryKind == settledKind)
                .Select(j => new { j.EntryKind, j.Direction, j.Amount })
                .ToListAsync(cancellationToken);

            var accrued = entries
                .Where(e => e.EntryKind == accruedKind && e.Direction == accruedDirection)
                .Sum(e => e.Amount);
            var settled = entries
                .Where(e => e.EntryKind == settledKind && e.Direction == settledDirection)
                .Sum(e => e.Amount);

            return Math.Max(0, accrued - settled);
        }

        public async Task<IReadOnlyList<OrderJournal>> GetOrderEntriesAsync(
            int orderId,
            CancellationToken cancellationToken = default)
        {
            return await _context.OrderJournals
                .AsNoTracking()
                .Where(j => j.OrderId == orderId)
                .OrderBy(j => j.CreatedDate)
                .ThenBy(j => j.OrderJournalId)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<OrderJournal>> GetPartyEntriesAsync(
            LedgerPartyType partyType,
            int partyId,
            CancellationToken cancellationToken = default)
        {
            return await _context.OrderJournals
                .AsNoTracking()
                .Where(j => j.PartyType == partyType && j.PartyId == partyId)
                .OrderByDescending(j => j.CreatedDate)
                .ThenByDescending(j => j.OrderJournalId)
                .ToListAsync(cancellationToken);
        }
    }
}
