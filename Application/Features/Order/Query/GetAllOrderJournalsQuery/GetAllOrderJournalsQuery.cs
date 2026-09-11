using Application.Features.Order.DTOs;
using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Query.GetAllOrderJournalsQuery
{
    /// <summary>
    /// Admin: list journal movements with optional filters.
    /// Totals (credit/debit/balance) are computed over the filtered visible set only.
    /// Balance = ΣCredit − ΣDebit (positive = ليه, negative = عليه).
    /// </summary>
    public record GetAllOrderJournalsQuery : IRequest<Result<OrderJournalListDto>>
    {
        public int? OrderId { get; set; }
        public int? DeliveryId { get; set; }
        public int? MerchantId { get; set; }
    }

    public class GetAllOrderJournalsQueryHandler : IRequestHandler<GetAllOrderJournalsQuery, Result<OrderJournalListDto>>
    {
        private readonly DatabaseContext _context;

        public GetAllOrderJournalsQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<OrderJournalListDto>> Handle(
            GetAllOrderJournalsQuery request,
            CancellationToken cancellationToken)
        {
            if (request.OrderId is <= 0)
                return Result.Failure<OrderJournalListDto>("OrderId must be greater than zero");
            if (request.DeliveryId is <= 0)
                return Result.Failure<OrderJournalListDto>("DeliveryId must be greater than zero");
            if (request.MerchantId is <= 0)
                return Result.Failure<OrderJournalListDto>("MerchantId must be greater than zero");

            if (request.DeliveryId.HasValue && request.MerchantId.HasValue)
                return Result.Failure<OrderJournalListDto>("Filter by Delivery or Merchant, not both");

            var query = _context.OrderJournals.AsNoTracking().AsQueryable();

            if (request.OrderId.HasValue)
                query = query.Where(j => j.OrderId == request.OrderId.Value);

            if (request.DeliveryId.HasValue)
            {
                query = query.Where(j =>
                    j.PartyType == LedgerPartyType.Delivery &&
                    j.PartyId == request.DeliveryId.Value);
            }

            if (request.MerchantId.HasValue)
            {
                query = query.Where(j =>
                    j.PartyType == LedgerPartyType.Merchant &&
                    j.PartyId == request.MerchantId.Value);
            }

            var rows = await query
                .OrderByDescending(j => j.CreatedDate)
                .ThenByDescending(j => j.OrderJournalId)
                .ToListAsync(cancellationToken);

            var merchantIds = rows
                .Where(j => j.PartyType == LedgerPartyType.Merchant && j.PartyId.HasValue)
                .Select(j => j.PartyId!.Value)
                .Distinct()
                .ToList();

            var deliveryIds = rows
                .Where(j => j.PartyType == LedgerPartyType.Delivery && j.PartyId.HasValue)
                .Select(j => j.PartyId!.Value)
                .Distinct()
                .ToList();

            var merchantNames = merchantIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Merchants.AsNoTracking()
                    .Where(m => merchantIds.Contains(m.MerchantId))
                    .ToDictionaryAsync(m => m.MerchantId, m => m.FullName, cancellationToken);

            var deliveryNames = deliveryIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Deliveries.AsNoTracking()
                    .Where(d => deliveryIds.Contains(d.DeliveryId))
                    .ToDictionaryAsync(d => d.DeliveryId, d => d.FullName, cancellationToken);

            decimal totalCredit = 0;
            decimal totalDebit = 0;
            var entries = new List<OrderJournalMovementDto>(rows.Count);

            foreach (var j in rows)
            {
                if (j.Direction == JournalDirection.Credit)
                    totalCredit += j.Amount;
                else
                    totalDebit += j.Amount;

                string? partyName = null;
                if (j.PartyType == LedgerPartyType.Merchant && j.PartyId.HasValue)
                    merchantNames.TryGetValue(j.PartyId.Value, out partyName);
                else if (j.PartyType == LedgerPartyType.Delivery && j.PartyId.HasValue)
                    deliveryNames.TryGetValue(j.PartyId.Value, out partyName);
                else if (j.PartyType == LedgerPartyType.Company)
                    partyName = "Company";

                entries.Add(new OrderJournalMovementDto
                {
                    OrderJournalId = j.OrderJournalId,
                    OrderId = j.OrderId,
                    PartyType = j.PartyType,
                    PartyId = j.PartyId,
                    PartyName = partyName,
                    Direction = j.Direction,
                    Amount = j.Amount,
                    EntryKind = j.EntryKind,
                    IdempotencyKey = j.IdempotencyKey,
                    FaultParty = j.FaultParty,
                    Note = j.Note,
                    CreatedBy = j.CreatedBy,
                    CreatedDate = j.CreatedDate
                });
            }

            return Result.Success(new OrderJournalListDto
            {
                Entries = entries,
                TotalCredit = totalCredit,
                TotalDebit = totalDebit,
                Balance = totalCredit - totalDebit
            });
        }
    }
}
