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

namespace Application.Features.Order.Command.DeliveryRemittanceToCompanyCommand
{
    /// <summary>
    /// Admin records that delivery cash collected for an order was remitted to Ecco.
    /// Not exposed on delivery/merchant apps — admin-only settlement screen.
    /// </summary>
    public record DeliveryRemittanceToCompanyCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
        public int DeliveryId { get; set; }
        public decimal? Amount { get; set; }
        public string? Reason { get; set; }
        public string? Note { get; set; }
    }

    public class DeliveryRemittanceToCompanyCommandHandler
        : IRequestHandler<DeliveryRemittanceToCompanyCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IOrderJournalService _journal;
        private readonly IOrderRealtimeNotifier _realtime;

        public DeliveryRemittanceToCompanyCommandHandler(
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

        public async Task<Result<bool>> Handle(
            DeliveryRemittanceToCompanyCommand request,
            CancellationToken cancellationToken)
        {
            if (request.DeliveryId <= 0)
                return Result.Failure<bool>("DeliveryId is required");

            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
                return Result.Failure<bool>($"Order with ID {request.OrderId} not found");

            var deliveryExists = await _context.Deliveries
                .AsNoTracking()
                .AnyAsync(d => d.DeliveryId == request.DeliveryId, cancellationToken);

            if (!deliveryExists)
                return Result.Failure<bool>($"Delivery with ID {request.DeliveryId} not found");

            var openCashDebt = await _journal.GetOpenAmountAsync(
                request.OrderId,
                LedgerPartyType.Delivery,
                request.DeliveryId,
                OrderJournalEntryKind.CashCollectedFromCustomer,
                OrderJournalEntryKind.DeliveryRemittanceToCompany,
                JournalDirection.Debit,
                JournalDirection.Credit,
                cancellationToken);

            if (openCashDebt <= 0)
                return Result.Failure<bool>("No open cash debt to remit for this delivery on the order");

            var amount = request.Amount ?? openCashDebt;
            if (amount <= 0)
                return Result.Failure<bool>("Remittance amount must be greater than zero");

            if (amount > openCashDebt)
                return Result.Failure<bool>($"Remittance amount exceeds open cash debt ({openCashDebt})");

            var createdBy = _userSession.UserName ?? "System";
            var note = !string.IsNullOrWhiteSpace(request.Note) ? request.Note : request.Reason;
            var key = OrderJournalKeys.Build(
                request.OrderId,
                $"delivery-remittance:{request.DeliveryId}:{amount:0.##}:{System.Guid.NewGuid():N}");

            var postResult = await _journal.PostCreditAsync(
                request.OrderId,
                LedgerPartyType.Delivery,
                request.DeliveryId,
                amount,
                OrderJournalEntryKind.DeliveryRemittanceToCompany,
                key,
                note: note,
                createdBy: createdBy,
                cancellationToken: cancellationToken);

            if (postResult.IsFailure)
                return Result.Failure<bool>(postResult.Error);

            _context.CompanyTreasuries.Add(
                TreasuryService.CreateDeliveryRemittanceRecord(
                    amount,
                    order.OrderCode,
                    request.DeliveryId,
                    createdBy));

            await _context.SaveChangesAsync(cancellationToken);

            await _realtime.NotifyAsync(
                request.OrderId,
                "Delivery remittance",
                $"Delivery remittance recorded on order #{order.OrderCode}.",
                NotificationType.OrderUpdated,
                cancellationToken: cancellationToken);

            return Result.Success(true);
        }
    }
}
