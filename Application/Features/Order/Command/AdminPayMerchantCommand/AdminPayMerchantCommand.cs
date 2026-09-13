using Application.Features.Order.DTOs;
using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Domain.Services;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.AdminPayMerchantCommand
{
    /// <summary>Admin pays merchant open credit on an order (not CashOnReceive-netted balances).</summary>
    public record AdminPayMerchantCommand : IRequest<Result<SettlementResultDto>>
    {
        public int MerchantId { get; set; }
        public int OrderId { get; set; }
        public decimal? Amount { get; set; }
        public string? Note { get; set; }
    }

    public class AdminPayMerchantCommandHandler : IRequestHandler<AdminPayMerchantCommand, Result<SettlementResultDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IOrderJournalService _journal;
        private readonly IOrderRealtimeNotifier _realtime;

        public AdminPayMerchantCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IOrderJournalService journal,
            IOrderRealtimeNotifier realtime)
        {
            _context = context;
            _userSession = userSession;
            _journal = journal;
            _realtime = realtime;
        }

        public async Task<Result<SettlementResultDto>> Handle(
            AdminPayMerchantCommand request,
            CancellationToken cancellationToken)
        {
            if (request.MerchantId <= 0)
                return Result.Failure<SettlementResultDto>("MerchantId is required");

            if (request.OrderId <= 0)
                return Result.Failure<SettlementResultDto>("OrderId is required");

            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
                return Result.Failure<SettlementResultDto>("Order not found");

            var merchantExists = await _context.Merchants
                .AsNoTracking()
                .AnyAsync(m => m.MerchantId == request.MerchantId && !m.IsDeleted, cancellationToken);

            if (!merchantExists)
                return Result.Failure<SettlementResultDto>("Merchant not found");

            var openBalance = await _journal.GetPartyBalanceAsync(
                request.OrderId,
                LedgerPartyType.Merchant,
                request.MerchantId,
                cancellationToken);

            if (openBalance <= 0)
                return Result.Failure<SettlementResultDto>("No open merchant credit to pay on this order");

            var amount = request.Amount ?? openBalance;
            if (amount <= 0)
                return Result.Failure<SettlementResultDto>("Amount must be greater than zero");

            if (amount > openBalance)
                return Result.Failure<SettlementResultDto>($"Amount exceeds open credit ({openBalance})");

            var actor = _userSession.UserName ?? "Admin";
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
                note: request.Note ?? "Merchant payout by company",
                createdBy: actor,
                cancellationToken: cancellationToken);

            if (postResult.IsFailure)
                return Result.Failure<SettlementResultDto>(postResult.Error);

            _context.CompanyTreasuries.Add(
                TreasuryService.CreateMerchantPayoutRecord(
                    amount, order.OrderCode, request.MerchantId, actor));

            await _context.SaveChangesAsync(cancellationToken);

            await _realtime.NotifyAsync(
                request.OrderId,
                "Merchant payout",
                $"Merchant payout recorded on order #{order.OrderCode}.",
                NotificationType.OrderUpdated,
                new[] { request.MerchantId },
                cancellationToken: cancellationToken);

            var balanceAfter = await _journal.GetGlobalPartyBalanceAsync(
                LedgerPartyType.Merchant, request.MerchantId, cancellationToken);

            return Result.Success(new SettlementResultDto
            {
                Success = true,
                PostedAmount = amount,
                PartyBalanceAfter = balanceAfter,
                Message = "Merchant payout recorded"
            });
        }
    }
}
