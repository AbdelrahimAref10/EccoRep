using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.ReassignMerchantOrderCommand
{
    public record ReassignMerchantOrderCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
        public int OldMerchantId { get; set; }
        public int NewMerchantId { get; set; }
    }

    public class ReassignMerchantOrderCommandHandler : IRequestHandler<ReassignMerchantOrderCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IOrderRealtimeNotifier _realtime;

        public ReassignMerchantOrderCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IOrderRealtimeNotifier realtime)
        {
            _context = context;
            _userSession = userSession;
            _realtime = realtime;
        }

        public async Task<Result<bool>> Handle(ReassignMerchantOrderCommand request, CancellationToken cancellationToken)
        {
            if (request.OldMerchantId <= 0 || request.NewMerchantId <= 0)
                return Result.Failure<bool>("Old and new merchant IDs are required");

            if (request.OldMerchantId == request.NewMerchantId)
                return Result.Failure<bool>("New merchant must be different from the old merchant");

            var order = await _context.Orders
                .AsTracking()
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
                return Result.Failure<bool>($"Order with ID {request.OrderId} not found");

            if (order.OrderState != OrderState.Pending
                && order.OrderState != OrderState.MerchantPending
                && order.OrderState != OrderState.MerchantConfirmed)
            {
                return Result.Failure<bool>(
                    $"Cannot reassign merchants after Confirmed. Current state: {order.OrderState}");
            }

            var newMerchantExists = await _context.Merchants
                .AsNoTracking()
                .AnyAsync(m => m.MerchantId == request.NewMerchantId && m.IsActive && !m.IsDeleted, cancellationToken);

            if (!newMerchantExists)
                return Result.Failure<bool>($"Active merchant with ID {request.NewMerchantId} not found");

            var oldInvitation = await _context.MerchantOrders
                .AsTracking()
                .FirstOrDefaultAsync(
                    mo => mo.OrderId == request.OrderId && mo.MerchantId == request.OldMerchantId,
                    cancellationToken);

            if (oldInvitation == null)
                return Result.Failure<bool>($"No merchant invitation found for merchant {request.OldMerchantId}");

            var modifiedBy = _userSession.UserName ?? "System";
            var wasMerchantConfirmed = order.OrderState == OrderState.MerchantConfirmed;

            if (oldInvitation.ResponseStatus is MerchantOrderResponseStatus.Accepted or MerchantOrderResponseStatus.PartiallyAccepted)
            {
                _context.MerchantOrders.Remove(oldInvitation);
            }
            else if (oldInvitation.ResponseStatus != MerchantOrderResponseStatus.Rejected)
            {
                oldInvitation.Reject("Reassigned to another merchant", modifiedBy);
            }

            var existingNew = await _context.MerchantOrders
                .AsTracking()
                .FirstOrDefaultAsync(
                    mo => mo.OrderId == request.OrderId && mo.MerchantId == request.NewMerchantId,
                    cancellationToken);

            if (existingNew != null)
            {
                existingNew.ResetToPending(modifiedBy);
            }
            else
            {
                await _context.MerchantOrders.AddAsync(
                    MerchantOrder.Create(request.OrderId, request.NewMerchantId, modifiedBy),
                    cancellationToken);
            }

            if (wasMerchantConfirmed || order.OrderState == OrderState.Pending)
            {
                order.MarkMerchantPending(modifiedBy);
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _realtime.NotifyAsync(
                order.OrderId,
                "Merchant reassigned",
                $"Order #{order.OrderCode} was reassigned to another merchant.",
                NotificationType.OrderMerchantPending,
                new[] { request.OldMerchantId, request.NewMerchantId },
                cancellationToken: cancellationToken);

            return Result.Success(true);
        }
    }
}
