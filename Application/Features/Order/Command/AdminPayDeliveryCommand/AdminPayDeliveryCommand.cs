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

namespace Application.Features.Order.Command.AdminPayDeliveryCommand
{
    /// <summary>Admin pays delivery: CashFloat (عُهدة) or OrderPayout (settle credit on order).</summary>
    public record AdminPayDeliveryCommand : IRequest<Result<SettlementResultDto>>
    {
        public int DeliveryId { get; set; }
        public AdminPayDeliveryKind Kind { get; set; }
        /// <summary>Required when Kind = OrderPayout.</summary>
        public int? OrderId { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
    }

    public class AdminPayDeliveryCommandHandler : IRequestHandler<AdminPayDeliveryCommand, Result<SettlementResultDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IOrderJournalService _journal;

        public AdminPayDeliveryCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IOrderJournalService journal)
        {
            _context = context;
            _userSession = userSession;
            _journal = journal;
        }

        public async Task<Result<SettlementResultDto>> Handle(
            AdminPayDeliveryCommand request,
            CancellationToken cancellationToken)
        {
            if (request.DeliveryId <= 0)
                return Result.Failure<SettlementResultDto>("DeliveryId is required");

            if (request.Amount <= 0)
                return Result.Failure<SettlementResultDto>("Amount must be greater than zero");

            var delivery = await _context.Deliveries
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DeliveryId == request.DeliveryId && d.IsActive && !d.IsDeleted, cancellationToken);

            if (delivery == null)
                return Result.Failure<SettlementResultDto>("Delivery not found or inactive");

            var actor = _userSession.UserName ?? "Admin";
            Result postResult;

            if (request.Kind == AdminPayDeliveryKind.CashFloat)
            {
                var key = OrderJournalKeys.BuildParty(
                    LedgerPartyType.Delivery,
                    request.DeliveryId,
                    $"cash-float:{request.Amount:0.##}:{System.Guid.NewGuid():N}");

                postResult = await _journal.PostDebitAsync(
                    orderId: null,
                    LedgerPartyType.Delivery,
                    request.DeliveryId,
                    request.Amount,
                    OrderJournalEntryKind.DeliveryCashFloatReceived,
                    key,
                    note: request.Note ?? "Cash float received from company",
                    createdBy: actor,
                    cancellationToken: cancellationToken);

                if (postResult.IsFailure)
                    return Result.Failure<SettlementResultDto>(postResult.Error);

                _context.CompanyTreasuries.Add(
                    TreasuryService.CreateDeliveryCashFloatOutRecord(
                        request.Amount, request.DeliveryId, actor));
            }
            else if (request.Kind == AdminPayDeliveryKind.OrderPayout)
            {
                if (!request.OrderId.HasValue || request.OrderId.Value <= 0)
                    return Result.Failure<SettlementResultDto>("OrderId is required for OrderPayout");

                var order = await _context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId.Value, cancellationToken);

                if (order == null)
                    return Result.Failure<SettlementResultDto>("Order not found");

                var openBalance = await _journal.GetPartyBalanceAsync(
                    request.OrderId.Value,
                    LedgerPartyType.Delivery,
                    request.DeliveryId,
                    cancellationToken);

                if (openBalance <= 0)
                    return Result.Failure<SettlementResultDto>("No open delivery credit to pay on this order");

                if (request.Amount > openBalance)
                    return Result.Failure<SettlementResultDto>($"Amount exceeds open credit ({openBalance})");

                var key = OrderJournalKeys.Build(
                    request.OrderId.Value,
                    $"delivery-paid-by-company:{request.DeliveryId}:{request.Amount:0.##}:{System.Guid.NewGuid():N}");

                postResult = await _journal.PostDebitAsync(
                    request.OrderId.Value,
                    LedgerPartyType.Delivery,
                    request.DeliveryId,
                    request.Amount,
                    OrderJournalEntryKind.DeliveryPaidByCompany,
                    key,
                    note: request.Note ?? "Delivery payout by company",
                    createdBy: actor,
                    cancellationToken: cancellationToken);

                if (postResult.IsFailure)
                    return Result.Failure<SettlementResultDto>(postResult.Error);

                _context.CompanyTreasuries.Add(
                    TreasuryService.CreateDeliveryPayoutRecord(
                        request.Amount, order.OrderCode, request.DeliveryId, actor));
            }
            else
            {
                return Result.Failure<SettlementResultDto>("Invalid payment kind");
            }

            await _context.SaveChangesAsync(cancellationToken);

            var balanceAfter = await _journal.GetGlobalPartyBalanceAsync(
                LedgerPartyType.Delivery, request.DeliveryId, cancellationToken);

            return Result.Success(new SettlementResultDto
            {
                Success = true,
                PostedAmount = request.Amount,
                PartyBalanceAfter = balanceAfter,
                Message = request.Kind == AdminPayDeliveryKind.CashFloat
                    ? "Cash float recorded against delivery"
                    : "Delivery order payout recorded"
            });
        }
    }
}
