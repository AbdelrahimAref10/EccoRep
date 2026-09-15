using Application.Features.Order.Common;
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

namespace Application.Features.Order.Command.AdminRemoveOrderVehicleCommand
{
    public record AdminRemoveOrderVehicleCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
        public int VehicleId { get; set; }
    }

    public class AdminRemoveOrderVehicleCommandHandler : IRequestHandler<AdminRemoveOrderVehicleCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IOrderRealtimeNotifier _realtime;

        public AdminRemoveOrderVehicleCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IOrderRealtimeNotifier realtime)
        {
            _context = context;
            _userSession = userSession;
            _realtime = realtime;
        }

        public async Task<Result<bool>> Handle(AdminRemoveOrderVehicleCommand request, CancellationToken cancellationToken)
        {
            if (request.VehicleId <= 0)
                return Result.Failure<bool>("Vehicle ID is required");

            var order = await _context.Orders
                .AsTracking()
                .Include(o => o.OrderPayments)
                .Include(o => o.OrderVehicles)
                    .ThenInclude(ov => ov.Vehicle)
                        .ThenInclude(v => v.Merchant)
                .Include(o => o.ReservedVehiclesPerDays)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
                return Result.Failure<bool>($"Order with ID {request.OrderId} not found");

            if (order.OrderState is not (
                OrderState.Pending or
                OrderState.MerchantPending or
                OrderState.MerchantConfirmed))
            {
                return Result.Failure<bool>(
                    $"Cannot remove vehicles after Confirmed. Current state: {order.OrderState}");
            }

            if (order.OrderVehicles.Count <= 1)
                return Result.Failure<bool>("Order must keep at least one vehicle");

            var link = order.OrderVehicles.FirstOrDefault(ov => ov.VehicleId == request.VehicleId);
            if (link == null)
                return Result.Failure<bool>("Vehicle is not on this order");

            var modifiedBy = _userSession.UserName ?? "Admin";

            _context.OrderVehicles.Remove(link);
            order.OrderVehicles.Remove(link);

            var oldReservations = order.ReservedVehiclesPerDays
                .Where(r => r.VehicleId == request.VehicleId)
                .ToList();
            foreach (var reservation in oldReservations)
                reservation.Cancel(modifiedBy);

            var stillBookedElsewhere = await _context.ReservedVehiclesPerDays
                .AsNoTracking()
                .AnyAsync(rv =>
                    rv.VehicleId == request.VehicleId
                    && rv.OrderId != request.OrderId
                    && rv.State == ReservedVehicleState.StillBooked
                    && rv.Order.OrderState != OrderState.Completed
                    && rv.Order.OrderState != OrderState.Cancelled,
                    cancellationToken);

            if (!stillBookedElsewhere)
                link.Vehicle.UpdateStatus(VehicleStatus.Available, modifiedBy);

            var remainingMerchantIds = order.OrderVehicles
                .Select(ov => ov.Vehicle.MerchantId)
                .Distinct()
                .ToHashSet();

            var invitations = await _context.MerchantOrders
                .AsTracking()
                .Where(mo => mo.OrderId == request.OrderId)
                .ToListAsync(cancellationToken);

            foreach (var invite in invitations.Where(i => !remainingMerchantIds.Contains(i.MerchantId)).ToList())
            {
                if (invite.ResponseStatus is MerchantOrderResponseStatus.Accepted or MerchantOrderResponseStatus.PartiallyAccepted)
                    _context.MerchantOrders.Remove(invite);
                else if (invite.ResponseStatus != MerchantOrderResponseStatus.Rejected)
                    invite.Reject("Vehicle removed — merchant no longer on order", modifiedBy);
            }

            foreach (var invite in invitations.Where(i => remainingMerchantIds.Contains(i.MerchantId)))
            {
                var merchantVehicles = order.OrderVehicles
                    .Where(ov => ov.Vehicle.MerchantId == invite.MerchantId)
                    .ToList();
                OrderFleetFinancialHelper.SyncInvitation(invite, merchantVehicles, modifiedBy);
            }

            try
            {
                await OrderFleetFinancialHelper.RecalculateFromAssignedVehiclesAsync(
                    _context,
                    order,
                    modifiedBy,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                return Result.Failure<bool>(ex.Message);
            }

            if (OrderFleetFinancialHelper.CanMarkMerchantConfirmed(order, invitations)
                && order.OrderState == OrderState.MerchantPending)
            {
                order.MarkMerchantConfirmed(modifiedBy);
            }
            else if (order.OrderState == OrderState.MerchantConfirmed)
            {
                order.MarkMerchantPending(modifiedBy);
            }

            var save = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!save.IsSuccess)
                return Result.Failure<bool>(save.ErrorMessage ?? "Failed to remove vehicle");

            await _realtime.NotifyAsync(
                order.OrderId,
                "Vehicle removed",
                $"A vehicle was removed from order #{order.OrderCode}.",
                NotificationType.OrderUpdated,
                cancellationToken: cancellationToken);

            return Result.Success(true);
        }
    }
}
