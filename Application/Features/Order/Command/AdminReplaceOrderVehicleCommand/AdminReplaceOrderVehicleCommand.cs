using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.AdminReplaceOrderVehicleCommand
{
    /// <summary>
    /// Admin replaces one order vehicle with another available in the order date range.
    /// Allowed while order is still Pending / MerchantPending / MerchantConfirmed.
    /// Syncs merchant invitations to match vehicles remaining on the order.
    /// </summary>
    public record AdminReplaceOrderVehicleCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
        public int OldVehicleId { get; set; }
        public int NewVehicleId { get; set; }
    }

    public class AdminReplaceOrderVehicleCommandHandler : IRequestHandler<AdminReplaceOrderVehicleCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public AdminReplaceOrderVehicleCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(AdminReplaceOrderVehicleCommand request, CancellationToken cancellationToken)
        {
            if (request.OldVehicleId <= 0 || request.NewVehicleId <= 0)
                return Result.Failure<bool>("Old and new vehicle IDs are required");

            if (request.OldVehicleId == request.NewVehicleId)
                return Result.Failure<bool>("New vehicle must be different from the old vehicle");

            var order = await _context.Orders
                .AsTracking()
                .Include(o => o.OrderVehicles)
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
                    $"Cannot replace vehicles after Confirmed. Current state: {order.OrderState}");
            }

            var oldLink = order.OrderVehicles.FirstOrDefault(ov => ov.VehicleId == request.OldVehicleId);
            if (oldLink == null)
                return Result.Failure<bool>("Old vehicle is not on this order");

            if (order.OrderVehicles.Any(ov => ov.VehicleId == request.NewVehicleId))
                return Result.Failure<bool>("New vehicle is already on this order");

            var vehicles = await _context.Vehicles
                .AsTracking()
                .Include(v => v.Merchant)
                .Where(v => v.VehicleId == request.OldVehicleId || v.VehicleId == request.NewVehicleId)
                .ToListAsync(cancellationToken);

            var oldVehicle = vehicles.FirstOrDefault(v => v.VehicleId == request.OldVehicleId);
            var newVehicle = vehicles.FirstOrDefault(v => v.VehicleId == request.NewVehicleId);

            if (oldVehicle == null || newVehicle == null)
                return Result.Failure<bool>("Vehicle not found");

            if (newVehicle.SubCategoryId != order.SubCategoryId)
                return Result.Failure<bool>("New vehicle must belong to the order subcategory");

            if (newVehicle.Status == VehicleStatus.UnderMaintenance)
                return Result.Failure<bool>("New vehicle is under maintenance");

            if (newVehicle.Merchant == null || newVehicle.Merchant.IsDeleted || !newVehicle.Merchant.IsActive)
                return Result.Failure<bool>("New vehicle merchant is not active");

            var from = order.ReservationDateFrom.Date;
            var to = order.ReservationDateTo.Date;

            var hasConflict = await _context.ReservedVehiclesPerDays
                .AsNoTracking()
                .AnyAsync(rv =>
                    rv.VehicleId == request.NewVehicleId
                    && rv.OrderId != request.OrderId
                    && rv.State == ReservedVehicleState.StillBooked
                    && rv.Order.OrderState != OrderState.Completed
                    && rv.Order.OrderState != OrderState.Cancelled
                    && rv.DateFrom <= to
                    && rv.DateTo >= from,
                    cancellationToken);

            if (hasConflict)
                return Result.Failure<bool>("New vehicle is not available in the order date range");

            var modifiedBy = _userSession.UserName ?? "Admin";

            var remainingVehicleIds = order.OrderVehicles
                .Where(ov => ov.VehicleId != request.OldVehicleId)
                .Select(ov => ov.VehicleId)
                .Append(request.NewVehicleId)
                .Distinct()
                .ToList();

            _context.OrderVehicles.Remove(oldLink);
            await _context.OrderVehicles.AddAsync(
                OrderVehicle.Create(order.OrderId, request.NewVehicleId, modifiedBy),
                cancellationToken);

            var oldReservations = order.ReservedVehiclesPerDays
                .Where(r => r.VehicleId == request.OldVehicleId)
                .ToList();
            foreach (var reservation in oldReservations)
                reservation.Cancel(modifiedBy);

            var currentDate = from;
            while (currentDate <= to)
            {
                await _context.ReservedVehiclesPerDays.AddAsync(
                    ReservedVehiclesPerDays.Create(
                        newVehicle.VehicleId,
                        order.SubCategoryId,
                        newVehicle.VehicleCode,
                        order.OrderId,
                        currentDate,
                        currentDate,
                        modifiedBy),
                    cancellationToken);
                currentDate = currentDate.AddDays(1);
            }

            newVehicle.UpdateStatus(VehicleStatus.Rented, modifiedBy);

            var oldStillBookedElsewhere = await _context.ReservedVehiclesPerDays
                .AsNoTracking()
                .AnyAsync(rv =>
                    rv.VehicleId == request.OldVehicleId
                    && rv.OrderId != request.OrderId
                    && rv.State == ReservedVehicleState.StillBooked
                    && rv.Order.OrderState != OrderState.Completed
                    && rv.Order.OrderState != OrderState.Cancelled,
                    cancellationToken);

            if (!oldStillBookedElsewhere)
                oldVehicle.UpdateStatus(VehicleStatus.Available, modifiedBy);

            var remainingMerchantIds = await _context.Vehicles
                .AsNoTracking()
                .Where(v => remainingVehicleIds.Contains(v.VehicleId))
                .Select(v => v.MerchantId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var invitations = await _context.MerchantOrders
                .AsTracking()
                .Where(mo => mo.OrderId == request.OrderId)
                .ToListAsync(cancellationToken);

            if (invitations.Count > 0 || order.OrderState != OrderState.Pending)
            {
                foreach (var invite in invitations.Where(i => !remainingMerchantIds.Contains(i.MerchantId)).ToList())
                {
                    if (invite.ResponseStatus == MerchantOrderResponseStatus.Accepted)
                        _context.MerchantOrders.Remove(invite);
                    else if (invite.ResponseStatus != MerchantOrderResponseStatus.Rejected)
                        invite.Reject("Vehicle replaced — merchant no longer on order", modifiedBy);
                }

                foreach (var merchantId in remainingMerchantIds)
                {
                    var existing = invitations.FirstOrDefault(i => i.MerchantId == merchantId);
                    if (existing == null)
                    {
                        await _context.MerchantOrders.AddAsync(
                            MerchantOrder.Create(request.OrderId, merchantId, modifiedBy),
                            cancellationToken);
                    }
                    else if (existing.ResponseStatus == MerchantOrderResponseStatus.Rejected
                             && merchantId == newVehicle.MerchantId)
                    {
                        existing.ResetToPending(modifiedBy);
                    }
                }

                if (order.OrderState is OrderState.MerchantConfirmed or OrderState.Pending)
                    order.MarkMerchantPending(modifiedBy);
            }

            var save = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!save.IsSuccess)
                return Result.Failure<bool>(save.ErrorMessage ?? "Failed to replace vehicle");

            return Result.Success(true);
        }
    }
}
