using Application.Features.Order.DTOs;
using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Query.GetPartyLedgerQuery
{
    public record GetPartyLedgerQuery : IRequest<Result<PartyLedgerDto>>
    {
        public LedgerPartyType PartyType { get; set; }
        public int PartyId { get; set; }
    }

    public class GetPartyLedgerQueryHandler : IRequestHandler<GetPartyLedgerQuery, Result<PartyLedgerDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IOrderJournalService _journal;

        public GetPartyLedgerQueryHandler(DatabaseContext context, IOrderJournalService journal)
        {
            _context = context;
            _journal = journal;
        }

        public async Task<Result<PartyLedgerDto>> Handle(GetPartyLedgerQuery request, CancellationToken cancellationToken)
        {
            if (request.PartyType is not (LedgerPartyType.Delivery or LedgerPartyType.Merchant))
                return Result.Failure<PartyLedgerDto>("PartyType must be Delivery or Merchant");

            if (request.PartyId <= 0)
                return Result.Failure<PartyLedgerDto>("PartyId is required");

            string partyName;
            if (request.PartyType == LedgerPartyType.Delivery)
            {
                var delivery = await _context.Deliveries
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DeliveryId == request.PartyId, cancellationToken);
                if (delivery == null)
                    return Result.Failure<PartyLedgerDto>("Delivery not found");
                partyName = delivery.FullName;
            }
            else
            {
                var merchant = await _context.Merchants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.MerchantId == request.PartyId, cancellationToken);
                if (merchant == null)
                    return Result.Failure<PartyLedgerDto>("Merchant not found");
                partyName = merchant.FullName;
            }

            var balance = await _journal.GetGlobalPartyBalanceAsync(
                request.PartyType, request.PartyId, cancellationToken);

            var entries = await _journal.GetPartyEntriesAsync(
                request.PartyType, request.PartyId, cancellationToken);

            return Result.Success(new PartyLedgerDto
            {
                PartyType = request.PartyType,
                PartyId = request.PartyId,
                PartyName = partyName,
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
