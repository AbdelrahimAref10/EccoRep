using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.SettleDeliveryPayoutCommand
{
    public record SettleDeliveryPayoutCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
        public int DeliveryId { get; set; }
        public decimal? Amount { get; set; }
    }

    public class SettleDeliveryPayoutCommandHandler : IRequestHandler<SettleDeliveryPayoutCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IOrderJournalService _journal;
        private readonly IOrderRealtimeNotifier _realtime;

        public SettleDeliveryPayoutCommandHandler(
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

        public async Task<Result<bool>> Handle(SettleDeliveryPayoutCommand request, CancellationToken cancellationToken)
        {
            if (request.DeliveryId <= 0)
                return Result.Failure<bool>("DeliveryId is required");

            var orderExists = await _context.Orders
                .AsNoTracking()
                .AnyAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (!orderExists)
                return Result.Failure<bool>($"Order with ID {request.OrderId} not found");

            var deliveryExists = await _context.Deliveries
                .AsNoTracking()
                .AnyAsync(d => d.DeliveryId == request.DeliveryId, cancellationToken);

            if (!deliveryExists)
                return Result.Failure<bool>($"Delivery with ID {request.DeliveryId} not found");

            var openBalance = await _journal.GetPartyBalanceAsync(
                request.OrderId,
                LedgerPartyType.Delivery,
                request.DeliveryId,
                cancellationToken);

            if (openBalance <= 0)
                return Result.Failure<bool>("No open delivery credit to settle");

            var amount = request.Amount ?? openBalance;
            if (amount <= 0)
                return Result.Failure<bool>("Settlement amount must be greater than zero");

            if (amount > openBalance)
                return Result.Failure<bool>($"Settlement amount exceeds open balance ({openBalance})");

            var createdBy = _userSession.UserName ?? "System";
            var key = OrderJournalKeys.Build(
                request.OrderId,
                $"delivery-paid-by-company:{request.DeliveryId}:{amount:0.##}:{System.Guid.NewGuid():N}");

            var postResult = await _journal.PostDebitAsync(
                request.OrderId,
                LedgerPartyType.Delivery,
                request.DeliveryId,
                amount,
                OrderJournalEntryKind.DeliveryPaidByCompany,
                key,
                note: "Delivery payout settlement",
                createdBy: createdBy,
                cancellationToken: cancellationToken);

            if (postResult.IsFailure)
                return Result.Failure<bool>(postResult.Error);

            await _context.SaveChangesAsync(cancellationToken);

            await _realtime.NotifyAsync(
                request.OrderId,
                "Delivery payout settled",
                $"Delivery payout was settled on order {request.OrderId}.",
                NotificationType.OrderUpdated,
                cancellationToken: cancellationToken);

            return Result.Success(true);
        }
    }
}
