using Application.Features.Order.DTOs;
using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Query.GetMyMerchantLedgerQuery
{
    /// <summary>Merchant portal: own party ledger (partyId forced from session).</summary>
    public record GetMyMerchantLedgerQuery : IRequest<Result<PartyLedgerDto>>;

    public class GetMyMerchantLedgerQueryHandler : IRequestHandler<GetMyMerchantLedgerQuery, Result<PartyLedgerDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IOrderJournalService _journal;

        public GetMyMerchantLedgerQueryHandler(
            DatabaseContext context,
            IUserSession userSession,
            IOrderJournalService journal)
        {
            _context = context;
            _userSession = userSession;
            _journal = journal;
        }

        public async Task<Result<PartyLedgerDto>> Handle(
            GetMyMerchantLedgerQuery request,
            CancellationToken cancellationToken)
        {
            var merchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId && !m.IsDeleted, cancellationToken);

            if (merchant == null)
                return Result.Failure<PartyLedgerDto>("Merchant profile not found for current user");

            var balance = await _journal.GetGlobalPartyBalanceAsync(
                LedgerPartyType.Merchant, merchant.MerchantId, cancellationToken);

            var entries = await _journal.GetPartyEntriesAsync(
                LedgerPartyType.Merchant, merchant.MerchantId, cancellationToken);

            return Result.Success(new PartyLedgerDto
            {
                PartyType = LedgerPartyType.Merchant,
                PartyId = merchant.MerchantId,
                PartyName = merchant.FullName,
                Balance = balance,
                AmountOwedToCompany = balance < 0 ? -balance : 0,
                AmountOwedByCompany = balance > 0 ? balance : 0,
                Entries = entries.Select(j => new OrderJournalDto
                {
                    OrderJournalId = j.OrderJournalId,
                    OrderId = j.OrderId,
                    PartyType = j.PartyType,
                    PartyId = j.PartyId,
                    Direction = j.Direction,
                    Amount = j.Amount,
                    EntryKind = j.EntryKind,
                    IdempotencyKey = j.IdempotencyKey,
                    FaultParty = j.FaultParty,
                    Note = j.Note,
                    CreatedBy = j.CreatedBy,
                    CreatedDate = j.CreatedDate
                }).ToList()
            });
        }
    }
}
