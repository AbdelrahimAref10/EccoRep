using CSharpFunctionalExtensions;
using Domain.Enums;
using Domain.Models;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Order.Common
{
    public static class OrderJournalHelper
    {
        public static async Task<bool> ExistsAsync(
            DatabaseContext context,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            return await context.OrderJournals
                .AnyAsync(j => j.IdempotencyKey == idempotencyKey, cancellationToken);
        }

        public static async Task AddIfMissingAsync(
            DatabaseContext context,
            OrderJournal entry,
            CancellationToken cancellationToken)
        {
            if (await ExistsAsync(context, entry.IdempotencyKey, cancellationToken))
                return;

            await context.OrderJournals.AddAsync(entry, cancellationToken);
        }

        public static decimal Balance(IEnumerable<OrderJournal> entries)
        {
            decimal credit = 0;
            decimal debit = 0;
            foreach (var e in entries)
            {
                if (e.Direction == JournalDirection.Credit)
                    credit += e.Amount;
                else
                    debit += e.Amount;
            }
            return credit - debit;
        }

        public static decimal OpenCredit(
            IEnumerable<OrderJournal> entries,
            OrderJournalEntryKind accruedKind,
            OrderJournalEntryKind paidKind)
        {
            var accrued = entries
                .Where(e => e.EntryKind == accruedKind && e.Direction == JournalDirection.Credit)
                .Sum(e => e.Amount);
            var paid = entries
                .Where(e => e.EntryKind == paidKind && e.Direction == JournalDirection.Debit)
                .Sum(e => e.Amount);
            return Math.Max(0, accrued - paid);
        }

        public static string Key(int orderId, string suffix) =>
            $"order:{orderId}:{suffix}";
    }
}
