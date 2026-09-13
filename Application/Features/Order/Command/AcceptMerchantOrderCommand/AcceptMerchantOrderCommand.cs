using Application.Features.Order.Common;
using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.AcceptMerchantOrderCommand
{
    public record AcceptMerchantOrderCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }

        /// <summary>
        /// Vehicle IDs this merchant confirms. Unchecked vehicles on this merchant are declined
        /// and stay on the order until admin replaces or removes them.
        /// </summary>
        public List<int>? ConfirmedVehicleIds { get; set; }
    }

    public class AcceptMerchantOrderCommandHandler : IRequestHandler<AcceptMerchantOrderCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IOrderRealtimeNotifier _realtime;

        public AcceptMerchantOrderCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IOrderRealtimeNotifier realtime)
        {
            _context = context;
            _userSession = userSession;
            _realtime = realtime;
        }

        public async Task<Result<bool>> Handle(AcceptMerchantOrderCommand request, CancellationToken cancellationToken)
        {
            var merchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId, cancellationToken);

            if (merchant == null)
                return Result.Failure<bool>("Merchant profile not found for current user");

            var order = await _context.Orders
                .AsTracking()
                .Include(o => o.OrderVehicles)
                    .ThenInclude(ov => ov.Vehicle)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
                return Result.Failure<bool>($"Order with ID {request.OrderId} not found");

            if (order.OrderState != OrderState.MerchantPending)
                return Result.Failure<bool>($"Cannot accept merchant invitation in {order.OrderState} state");

            var merchantOrders = await _context.MerchantOrders
                .AsTracking()
                .Where(mo => mo.OrderId == request.OrderId)
                .ToListAsync(cancellationToken);

            var invitation = merchantOrders.FirstOrDefault(mo => mo.MerchantId == merchant.MerchantId);
            if (invitation == null)
                return Result.Failure<bool>("No invitation found for this merchant on the order");

            if (invitation.ResponseStatus == MerchantOrderResponseStatus.Rejected)
                return Result.Failure<bool>("Cannot accept a rejected merchant order invitation");

            var merchantVehicles = order.OrderVehicles
                .Where(ov => ov.Vehicle.MerchantId == merchant.MerchantId)
                .ToList();

            if (merchantVehicles.Count == 0)
                return Result.Failure<bool>("No vehicles on this order belong to the current merchant");

            var pendingVehicles = merchantVehicles
                .Where(ov => ov.MerchantResponseStatus == MerchantVehicleResponseStatus.Pending)
                .ToList();

            if (pendingVehicles.Count == 0)
                return Result.Failure<bool>("There are no pending vehicles left to confirm");

            if (request.ConfirmedVehicleIds == null || request.ConfirmedVehicleIds.Count == 0)
                return Result.Failure<bool>("Select at least one vehicle to confirm");

            var confirmedIds = request.ConfirmedVehicleIds.Distinct().ToHashSet();
            var pendingIds = pendingVehicles.Select(v => v.VehicleId).ToHashSet();

            if (confirmedIds.Any(id => !pendingIds.Contains(id)))
                return Result.Failure<bool>("One or more confirmed vehicles are not pending for this merchant");

            var modifiedBy = _userSession.UserName ?? "System";

            foreach (var ov in pendingVehicles)
            {
                if (confirmedIds.Contains(ov.VehicleId))
                    ov.ConfirmByMerchant(modifiedBy);
                else
                    ov.DeclineByMerchant(modifiedBy);
            }

            OrderFleetFinancialHelper.SyncInvitation(invitation, merchantVehicles, modifiedBy);

            if (OrderFleetFinancialHelper.CanMarkMerchantConfirmed(order, merchantOrders))
            {
                try
                {
                    order.MarkMerchantConfirmed(modifiedBy);
                }
                catch (InvalidOperationException ex)
                {
                    return Result.Failure<bool>(ex.Message);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            var declinedCount = merchantVehicles.Count(v => v.MerchantResponseStatus == MerchantVehicleResponseStatus.Declined);
            var title = declinedCount > 0 ? "Merchant declined vehicles" : "Merchant confirmation";
            var message = declinedCount > 0
                ? $"Merchant {merchant.FullName} confirmed {confirmedIds.Count} vehicle(s) on order #{order.OrderCode} and declined {declinedCount}."
                : OrderFleetFinancialHelper.CanMarkMerchantConfirmed(order, merchantOrders)
                    ? $"Merchant {merchant.FullName} accepted order #{order.OrderCode}. All merchants confirmed."
                    : $"Merchant {merchant.FullName} accepted order #{order.OrderCode}. Waiting for other merchants.";

            await _realtime.NotifyAsync(
                order.OrderId,
                title,
                message,
                NotificationType.OrderMerchantPending,
                cancellationToken: cancellationToken);

            return Result.Success(true);
        }
    }
}
