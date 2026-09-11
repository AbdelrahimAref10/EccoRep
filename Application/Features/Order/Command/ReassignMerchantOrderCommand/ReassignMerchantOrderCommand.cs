using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Infrastructure;
using Infrastructure.Services;
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
        private readonly IMerchantNotificationHubService _merchantNotifications;

        public ReassignMerchantOrderCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IMerchantNotificationHubService merchantNotifications)
        {
            _context = context;
            _userSession = userSession;
            _merchantNotifications = merchantNotifications;
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

            if (oldInvitation.ResponseStatus == MerchantOrderResponseStatus.Accepted)
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

            await _merchantNotifications.SendToMerchantsAsync(
                new[] { request.NewMerchantId },
                "New order invitation",
                $"Order #{order.OrderCode} is waiting for your confirmation.",
                NotificationType.OrderMerchantPending,
                order.OrderId,
                order.OrderCode);

            return Result.Success(true);
        }
    }
}
