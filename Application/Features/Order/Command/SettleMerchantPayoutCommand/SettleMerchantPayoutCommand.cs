using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.SettleMerchantPayoutCommand
{
    public record SettleMerchantPayoutCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
        public int MerchantId { get; set; }
        public decimal? Amount { get; set; }
    }

    public class SettleMerchantPayoutCommandHandler : IRequestHandler<SettleMerchantPayoutCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IOrderJournalService _journal;

        public SettleMerchantPayoutCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IOrderJournalService journal)
        {
            _context = context;
            _userSession = userSession;
            _journal = journal;
        }

        public async Task<Result<bool>> Handle(SettleMerchantPayoutCommand request, CancellationToken cancellationToken)
        {
            if (request.MerchantId <= 0)
                return Result.Failure<bool>("MerchantId is required");

            var orderExists = await _context.Orders
                .AsNoTracking()
                .AnyAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (!orderExists)
                return Result.Failure<bool>($"Order with ID {request.OrderId} not found");

            var merchantExists = await _context.Merchants
                .AsNoTracking()
                .AnyAsync(m => m.MerchantId == request.MerchantId, cancellationToken);

            if (!merchantExists)
                return Result.Failure<bool>($"Merchant with ID {request.MerchantId} not found");

            var openBalance = await _journal.GetPartyBalanceAsync(
                request.OrderId,
                LedgerPartyType.Merchant,
                request.MerchantId,
                cancellationToken);

            if (openBalance <= 0)
                return Result.Failure<bool>("No open merchant credit to settle");

            var amount = request.Amount ?? openBalance;
            if (amount <= 0)
                return Result.Failure<bool>("Settlement amount must be greater than zero");

            if (amount > openBalance)
                return Result.Failure<bool>($"Settlement amount exceeds open balance ({openBalance})");

            var createdBy = _userSession.UserName ?? "System";
            var key = OrderJournalKeys.Build(
                request.OrderId,
                $"merchant-paid-by-company:{request.MerchantId}:{amount:0.##}:{System.Guid.NewGuid():N}");

            var postResult = await _journal.PostDebitAsync(
                request.OrderId,
                LedgerPartyType.Merchant,
                request.MerchantId,
                amount,
                OrderJournalEntryKind.MerchantPaidByCompany,
                key,
                note: "Merchant payout settlement",
                createdBy: createdBy,
                cancellationToken: cancellationToken);

            if (postResult.IsFailure)
                return Result.Failure<bool>(postResult.Error);

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(true);
        }
    }
}
