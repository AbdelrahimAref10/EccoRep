using Application.Features.Order.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Query.GetMyMerchantJournalsQuery
{
    /// <summary>Merchant portal: own journal movements (merchantId forced from session).</summary>
    public record GetMyMerchantJournalsQuery : IRequest<Result<OrderJournalListDto>>
    {
        public int? OrderId { get; set; }
    }

    public class GetMyMerchantJournalsQueryHandler
        : IRequestHandler<GetMyMerchantJournalsQuery, Result<OrderJournalListDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public GetMyMerchantJournalsQueryHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<OrderJournalListDto>> Handle(
            GetMyMerchantJournalsQuery request,
            CancellationToken cancellationToken)
        {
            if (request.OrderId is <= 0)
                return Result.Failure<OrderJournalListDto>("OrderId must be greater than zero");

            var merchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId && !m.IsDeleted, cancellationToken);

            if (merchant == null)
                return Result.Failure<OrderJournalListDto>("Merchant profile not found for current user");

            var query = _context.OrderJournals
                .AsNoTracking()
                .Where(j =>
                    j.PartyType == LedgerPartyType.Merchant &&
                    j.PartyId == merchant.MerchantId);

            if (request.OrderId.HasValue)
                query = query.Where(j => j.OrderId == request.OrderId.Value);

            var rows = await query
                .OrderByDescending(j => j.CreatedDate)
                .ThenByDescending(j => j.OrderJournalId)
                .ToListAsync(cancellationToken);

            decimal totalCredit = 0;
            decimal totalDebit = 0;
            var entries = new List<OrderJournalMovementDto>(rows.Count);

            foreach (var j in rows)
            {
                if (j.Direction == JournalDirection.Credit)
                    totalCredit += j.Amount;
                else
                    totalDebit += j.Amount;

                entries.Add(new OrderJournalMovementDto
                {
                    OrderJournalId = j.OrderJournalId,
                    OrderId = j.OrderId,
                    PartyType = j.PartyType,
                    PartyId = j.PartyId,
                    PartyName = merchant.FullName,
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
