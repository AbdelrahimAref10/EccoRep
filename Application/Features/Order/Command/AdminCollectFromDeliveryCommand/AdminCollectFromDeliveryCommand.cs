using Application.Features.Order.DTOs;
using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Domain.Services;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.AdminCollectFromDeliveryCommand
{
    /// <summary>Admin collects from delivery: order cash remittance or float return.</summary>
    public record AdminCollectFromDeliveryCommand : IRequest<Result<SettlementResultDto>>
    {
        public int DeliveryId { get; set; }
        public AdminCollectFromDeliveryKind Kind { get; set; }
        /// <summary>Required when Kind = OrderCashRemittance.</summary>
        public int? OrderId { get; set; }
        public decimal Amount { get; set; }
        public string? Reason { get; set; }
        public string? Note { get; set; }
    }

    public class AdminCollectFromDeliveryCommandHandler
        : IRequestHandler<AdminCollectFromDeliveryCommand, Result<SettlementResultDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IOrderJournalService _journal;
        private readonly IOrderRealtimeNotifier _realtime;

        public AdminCollectFromDeliveryCommandHandler(
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
            AdminCollectFromDeliveryCommand request,
            CancellationToken cancellationToken)
        {
            if (request.DeliveryId <= 0)
                return Result.Failure<SettlementResultDto>("DeliveryId is required");

            if (request.Amount <= 0)
                return Result.Failure<SettlementResultDto>("Amount must be greater than zero");

            var deliveryExists = await _context.Deliveries
                .AsNoTracking()
                .AnyAsync(d => d.DeliveryId == request.DeliveryId, cancellationToken);

            if (!deliveryExists)
                return Result.Failure<SettlementResultDto>("Delivery not found");

            var actor = _userSession.UserName ?? "Admin";
            var note = !string.IsNullOrWhiteSpace(request.Note) ? request.Note : request.Reason;
            Result postResult;

            if (request.Kind == AdminCollectFromDeliveryKind.OrderCashRemittance)
            {
                if (!request.OrderId.HasValue || request.OrderId.Value <= 0)
                    return Result.Failure<SettlementResultDto>("OrderId is required for OrderCashRemittance");

                var order = await _context.Orders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId.Value, cancellationToken);

                if (order == null)
                    return Result.Failure<SettlementResultDto>("Order not found");

                var openCashDebt = await _journal.GetOpenAmountAsync(
                    request.OrderId.Value,
                    LedgerPartyType.Delivery,
                    request.DeliveryId,
                    OrderJournalEntryKind.CashCollectedFromCustomer,
                    OrderJournalEntryKind.DeliveryRemittanceToCompany,
                    JournalDirection.Debit,
                    JournalDirection.Credit,
                    cancellationToken);

                if (openCashDebt <= 0)
                    return Result.Failure<SettlementResultDto>("No open cash debt to collect for this delivery on the order");

                if (request.Amount > openCashDebt)
                    return Result.Failure<SettlementResultDto>($"Amount exceeds open cash debt ({openCashDebt})");

                var key = OrderJournalKeys.Build(
                    request.OrderId.Value,
                    $"delivery-remittance:{request.DeliveryId}:{request.Amount:0.##}:{System.Guid.NewGuid():N}");

                postResult = await _journal.PostCreditAsync(
                    request.OrderId.Value,
                    LedgerPartyType.Delivery,
                    request.DeliveryId,
                    request.Amount,
                    OrderJournalEntryKind.DeliveryRemittanceToCompany,
                    key,
                    note: note ?? "Order cash remittance to company",
                    createdBy: actor,
                    cancellationToken: cancellationToken);

                if (postResult.IsFailure)
                    return Result.Failure<SettlementResultDto>(postResult.Error);

                _context.CompanyTreasuries.Add(
                    TreasuryService.CreateDeliveryRemittanceRecord(
                        request.Amount, order.OrderCode, request.DeliveryId, actor));
            }
            else if (request.Kind == AdminCollectFromDeliveryKind.FloatReturn)
            {
                var openFloat = await GetOpenFloatAsync(request.DeliveryId, cancellationToken);
                if (openFloat <= 0)
                    return Result.Failure<SettlementResultDto>("No open cash float to return for this delivery");

                if (request.Amount > openFloat)
                    return Result.Failure<SettlementResultDto>($"Amount exceeds open float ({openFloat})");

                var key = OrderJournalKeys.BuildParty(
                    LedgerPartyType.Delivery,
                    request.DeliveryId,
                    $"cash-float-return:{request.Amount:0.##}:{System.Guid.NewGuid():N}");

                postResult = await _journal.PostCreditAsync(
                    orderId: null,
                    LedgerPartyType.Delivery,
                    request.DeliveryId,
                    request.Amount,
                    OrderJournalEntryKind.DeliveryCashFloatReturned,
                    key,
                    note: note ?? "Cash float returned to company",
                    createdBy: actor,
                    cancellationToken: cancellationToken);

                if (postResult.IsFailure)
                    return Result.Failure<SettlementResultDto>(postResult.Error);

                _context.CompanyTreasuries.Add(
                    TreasuryService.CreateDeliveryCashFloatReturnRecord(
                        request.Amount, request.DeliveryId, actor));
            }
            else
            {
                return Result.Failure<SettlementResultDto>("Invalid collection kind");
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (request.OrderId is > 0)
            {
                await _realtime.NotifyAsync(
                    request.OrderId.Value,
                    "Delivery collection",
                    $"Delivery collection recorded on order {request.OrderId}.",
                    NotificationType.OrderUpdated,
                    cancellationToken: cancellationToken);
            }

            var balanceAfter = await _journal.GetGlobalPartyBalanceAsync(
                LedgerPartyType.Delivery, request.DeliveryId, cancellationToken);

            return Result.Success(new SettlementResultDto
            {
                Success = true,
                PostedAmount = request.Amount,
                PartyBalanceAfter = balanceAfter,
                Message = request.Kind == AdminCollectFromDeliveryKind.FloatReturn
                    ? "Cash float return recorded"
                    : "Order cash remittance recorded"
            });
        }

        private async Task<decimal> GetOpenFloatAsync(int deliveryId, CancellationToken cancellationToken)
        {
            var entries = await _context.OrderJournals.AsNoTracking()
                .Where(j => j.PartyType == LedgerPartyType.Delivery
                    && j.PartyId == deliveryId
                    && j.OrderId == null
                    && (j.EntryKind == OrderJournalEntryKind.DeliveryCashFloatReceived
                        || j.EntryKind == OrderJournalEntryKind.DeliveryCashFloatReturned))
                .Select(j => new { j.EntryKind, j.Amount })
                .ToListAsync(cancellationToken);

            var received = entries
                .Where(e => e.EntryKind == OrderJournalEntryKind.DeliveryCashFloatReceived)
                .Sum(e => e.Amount);
            var returned = entries
                .Where(e => e.EntryKind == OrderJournalEntryKind.DeliveryCashFloatReturned)
                .Sum(e => e.Amount);

            return Math.Max(0, received - returned);
        }
    }
}
