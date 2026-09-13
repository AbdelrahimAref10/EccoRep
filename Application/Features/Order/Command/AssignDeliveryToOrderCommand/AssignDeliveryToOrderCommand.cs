using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.AssignDeliveryToOrderCommand
{
    public record AssignDeliveryVehicleItem
    {
        public int VehicleId { get; set; }
        public int DeliveryId { get; set; }
    }

    public record AssignDeliveryToOrderCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
        public List<AssignDeliveryVehicleItem> Assignments { get; set; } = new();
    }

    public class AssignDeliveryToOrderCommandHandler : IRequestHandler<AssignDeliveryToOrderCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IOrderRealtimeNotifier _realtime;

        public AssignDeliveryToOrderCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IOrderRealtimeNotifier realtime)
        {
            _context = context;
            _userSession = userSession;
            _realtime = realtime;
        }

        public async Task<Result<bool>> Handle(AssignDeliveryToOrderCommand request, CancellationToken cancellationToken)
        {
            if (request.Assignments == null || request.Assignments.Count == 0)
                return Result.Failure<bool>("At least one vehicle/delivery assignment is required");

            if (request.Assignments.Select(a => a.VehicleId).Distinct().Count() != request.Assignments.Count)
                return Result.Failure<bool>("Duplicate vehicle IDs are not allowed");

            var order = await _context.Orders
                .AsTracking()
                .Include(o => o.OrderVehicles)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
                return Result.Failure<bool>($"Order with ID {request.OrderId} not found");

            if (order.OrderState != OrderState.Confirmed && order.OrderState != OrderState.DeliveryAssigned)
                return Result.Failure<bool>($"Cannot assign delivery in {order.OrderState} state. Order must be Confirmed");

            var orderVehicleIds = order.OrderVehicles.Select(ov => ov.VehicleId).ToHashSet();
            var requestedVehicleIds = request.Assignments.Select(a => a.VehicleId).ToList();

            if (requestedVehicleIds.Any(id => !orderVehicleIds.Contains(id)))
                return Result.Failure<bool>("One or more vehicles are not assigned to this order");

            var deliveryIds = request.Assignments.Select(a => a.DeliveryId).Distinct().ToList();
            var deliveries = await _context.Deliveries
                .AsNoTracking()
                .Where(d => deliveryIds.Contains(d.DeliveryId) && d.IsActive && !d.IsDeleted)
                .Select(d => new { d.DeliveryId, d.CityId })
                .ToListAsync(cancellationToken);

            if (deliveries.Count != deliveryIds.Count)
                return Result.Failure<bool>("One or more deliveries were not found or are inactive");

            if (deliveries.Any(d => d.CityId != order.CityId))
                return Result.Failure<bool>("All deliveries must belong to the same city as the order");

            var orderTotals = await _context.OrderTotals
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.OrderId == request.OrderId, cancellationToken);

            if (orderTotals == null)
                return Result.Failure<bool>("Order totals not found for this order");

            if (order.VehiclesCount <= 0)
                return Result.Failure<bool>("Order vehicles count is invalid");

            var feeShare = orderTotals.DeliveryFees / order.VehiclesCount;
            var createdBy = _userSession.UserName ?? "System";

            var existingAssignments = await _context.DeliveryMenOrders
                .AsTracking()
                .Where(d => d.OrderId == request.OrderId && requestedVehicleIds.Contains(d.VehicleId))
                .ToListAsync(cancellationToken);

            var existingPaymentDetails = await _context.DeliveryOrderPaymentDetails
                .AsTracking()
                .Where(d => d.OrderId == request.OrderId && requestedVehicleIds.Contains(d.VehicleId))
                .ToListAsync(cancellationToken);

            foreach (var assignment in request.Assignments)
            {
                var existing = existingAssignments.FirstOrDefault(d => d.VehicleId == assignment.VehicleId);
                if (existing != null)
                {
                    if (existing.DeliveryReceivedFromMerchant)
                    {
                        return Result.Failure<bool>(
                            $"Vehicle {assignment.VehicleId} already handed over to delivery and cannot be reassigned");
                    }

                    if (existing.DeliveryId != assignment.DeliveryId)
                    {
                        _context.DeliveryMenOrders.Remove(existing);
                        await _context.DeliveryMenOrders.AddAsync(
                            DeliveryMenOrder.Create(request.OrderId, assignment.VehicleId, assignment.DeliveryId, createdBy),
                            cancellationToken);
                    }
                }
                else
                {
                    await _context.DeliveryMenOrders.AddAsync(
                        DeliveryMenOrder.Create(request.OrderId, assignment.VehicleId, assignment.DeliveryId, createdBy),
                        cancellationToken);
                }

                var paymentDetail = existingPaymentDetails.FirstOrDefault(d => d.VehicleId == assignment.VehicleId);
                if (paymentDetail != null)
                {
                    if (paymentDetail.DeliveryId != assignment.DeliveryId)
                    {
                        _context.DeliveryOrderPaymentDetails.Remove(paymentDetail);
                        await _context.DeliveryOrderPaymentDetails.AddAsync(
                            DeliveryOrderPaymentDetail.Create(
                                request.OrderId,
                                assignment.DeliveryId,
                                assignment.VehicleId,
                                feeShare,
                                createdBy),
                            cancellationToken);
                    }
                }
                else
                {
                    await _context.DeliveryOrderPaymentDetails.AddAsync(
                        DeliveryOrderPaymentDetail.Create(
                            request.OrderId,
                            assignment.DeliveryId,
                            assignment.VehicleId,
                            feeShare,
                            createdBy),
                        cancellationToken);
                }
            }

            // After loop inserts, ensure full order vehicle coverage then move to DeliveryAssigned
            var existingOther = await _context.DeliveryMenOrders
                .AsNoTracking()
                .Where(d => d.OrderId == request.OrderId && !requestedVehicleIds.Contains(d.VehicleId))
                .Select(d => d.VehicleId)
                .ToListAsync(cancellationToken);

            var coveredVehicleIds = existingOther
                .Concat(requestedVehicleIds)
                .Distinct()
                .ToHashSet();

            if (coveredVehicleIds.Count != orderVehicleIds.Count
                || orderVehicleIds.Any(id => !coveredVehicleIds.Contains(id)))
            {
                return Result.Failure<bool>("All order vehicles must be assigned to a delivery");
            }

            order.MarkDeliveryAssigned(createdBy);
            await _context.SaveChangesAsync(cancellationToken);

            await _realtime.NotifyAsync(
                order.OrderId,
                "Delivery assigned",
                $"Delivery was assigned on order #{order.OrderCode}.",
                NotificationType.OrderUpdated,
                cancellationToken: cancellationToken);

            return Result.Success(true);
        }
    }
}
