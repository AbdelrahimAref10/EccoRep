using Application.Features.Order.Common;
using Application.Features.Order.DTOs;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
using Domain.Services;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.UpdateOrderStateCommand
{
    public record UpdateOrderStateCommand : IRequest<Result<OrderDto>>
    {
        public int OrderId { get; set; }
        public OrderState NewState { get; set; }
        public List<int>? VehicleIds { get; set; } // For confirmation only
    }

    public class UpdateOrderStateCommandHandler : IRequestHandler<UpdateOrderStateCommand, Result<OrderDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly INotificationService _notificationService;
        private readonly IAdminNotificationHubService _adminNotificationHubService;

        public UpdateOrderStateCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            INotificationService notificationService,
            IAdminNotificationHubService adminNotificationHubService)
        {
            _context = context;
            _userSession = userSession;
            _notificationService = notificationService;
            _adminNotificationHubService = adminNotificationHubService;
        }

        public async Task<Result<OrderDto>> Handle(UpdateOrderStateCommand request, CancellationToken cancellationToken)
        {
            OrderPayment orderPayment;
            var order = await _context.Orders
                .AsTracking()
                .Include(o => o.Customer)
                .Include(o => o.SubCategory)
                .Include(o => o.City)
                .Include(o => o.OrderVehicles)
                    .ThenInclude(ov => ov.Vehicle)
                .Include(o => o.OrderPayments)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                return Result.Failure<OrderDto>($"Order with ID {request.OrderId} not found");
            }

            if (order.OrderState == OrderState.Cancelled
                || await CancellationDebtHelper.IsOrderCancelledAsync(_context, order, cancellationToken))
            {
                return Result.Failure<OrderDto>("Cannot update state of a cancelled order");
            }

            // Update state based on transition
            switch (request.NewState)
            {
                case OrderState.Confirmed:
                    if (order.OrderState != OrderState.Pending)
                    {
                        return Result.Failure<OrderDto>($"Cannot confirm order. Current state: {order.OrderState}");
                    }

                    // Validate vehicle IDs are provided and match exact required count
                    if (request.VehicleIds == null || request.VehicleIds.Count == 0
                        || request.VehicleIds.Count != order.VehiclesCount
                        || request.VehicleIds.Distinct().Count() != request.VehicleIds.Count)
                    {
                        return Result.Failure<OrderDto>($"Please select {order.VehiclesCount} vehicles");
                    }

                    // Get and validate vehicles (shared domain availability rule)
                    var vehicles = await _context.Vehicles
                        .AsTracking()
                        .Where(v => request.VehicleIds.Contains(v.VehicleId))
                        .ToListAsync(cancellationToken);

                    if (vehicles.Count != request.VehicleIds.Count)
                    {
                        return Result.Failure<OrderDto>("One or more vehicles not found");
                    }

                    var from = order.ReservationDateFrom.Date;
                    var to = order.ReservationDateTo.Date;

                    var stillBookedOverlaps = await _context.ReservedVehiclesPerDays
                        .AsNoTracking()
                        .Include(rv => rv.Order)
                        .Where(rv => request.VehicleIds.Contains(rv.VehicleId)
                            && rv.State == ReservedVehicleState.StillBooked
                            && rv.Order.OrderState != OrderState.Completed && rv.Order.OrderState != OrderState.Cancelled
                            && rv.DateFrom <= to
                            && rv.DateTo >= from)
                        .Select(rv => new ValueTuple<int, DateTime, DateTime>(rv.VehicleId, rv.DateFrom, rv.DateTo))
                        .ToListAsync(cancellationToken);

                    var availability = Domain.Models.Order.EnsureSelectedVehiclesAreAvailable(
                        vehicles,
                        order.SubCategoryId,
                        from,
                        to,
                        stillBookedOverlaps);

                    if (availability.IsFailure)
                    {
                        return Result.Failure<OrderDto>(availability.Error);
                    }

                    // Confirm order
                    order.Confirm(_userSession.UserName ?? "System");

                    // Create OrderVehicle records
                    foreach (var vehicleId in request.VehicleIds)
                    {
                        var orderVehicle = Domain.Models.OrderVehicle.Create(
                            order.OrderId,
                            vehicleId,
                            _userSession.UserName ?? "System"
                        );
                        _context.OrderVehicles.Add(orderVehicle);
                    }

                    // Create ReservedVehiclesPerDays records (one per vehicle per day)
                    foreach (var vehicle in vehicles)
                    {
                        var currentDate = order.ReservationDateFrom.Date;
                        var endDate = order.ReservationDateTo.Date;

                        while (currentDate <= endDate)
                        {
                            var reserved = Domain.Models.ReservedVehiclesPerDays.Create(
                                vehicle.VehicleId,
                                order.SubCategoryId,
                                vehicle.VehicleCode,
                                order.OrderId,
                                currentDate,
                                currentDate,
                                _userSession.UserName ?? "System"
                            );
                            _context.ReservedVehiclesPerDays.Add(reserved);
                            currentDate = currentDate.AddDays(1);
                        }

                        // Update vehicle status to Rented
                        vehicle.UpdateStatus(VehicleStatus.Rented, _userSession.UserName ?? "System");
                    }

                    // Note: PayPal payment treasury record creation is removed - will be handled automatically when payment is successful
                    break;

                case OrderState.OnWay:
                    if (order.OrderState != OrderState.Confirmed)
                    {
                        return Result.Failure<OrderDto>($"Cannot mark order as OnWay. Current state: {order.OrderState}");
                    }
                    order.MarkOnWay(_userSession.UserName ?? "System");
                    break;

                case OrderState.CustomerReceived:
                    if (order.OrderState != OrderState.OnWay)
                    {
                        return Result.Failure<OrderDto>($"Cannot mark customer received. Current state: {order.OrderState}");
                    }
                    order.MarkCustomerReceived(_userSession.UserName ?? "System");

                    // If payment method is Cash, automatically mark payment as Paid and create treasury record
                    if (order.PaymentMethodId == (int)PaymentMethod.Cash)
                    {
                        orderPayment = order.OrderPayments.FirstOrDefault();
                        if (orderPayment != null && orderPayment.State == PaymentState.Pending)
                        {
                            orderPayment.MarkAsPaid(_userSession.UserName ?? "System");

                            // Create treasury record for cash payment
                            var treasuryRecord = TreasuryService.CreateCashPaymentRecord(
                                orderPayment.Total,
                                order.OrderCode,
                                _userSession.UserName ?? "System");
                            _context.CompanyTreasuries.Add(treasuryRecord);
                        }

                        // Settle previous cancellation debt collected with this order
                        await CancellationDebtHelper.MarkUnderPaymentFeesAsPaidAsync(
                            _context,
                            order.OrderId,
                            cancellationToken);
                    }
                    break;

                case OrderState.Completed:
                    if (order.OrderState != OrderState.CustomerReceived)
                    {
                        return Result.Failure<OrderDto>($"Cannot complete order. Current state: {order.OrderState}");
                    }
                    // Update vehicle status to Available
                    foreach (var orderVehicle in order.OrderVehicles)
                    {
                        orderVehicle.Vehicle.UpdateStatus(VehicleStatus.Available, _userSession.UserName ?? "System");
                    }
                    order.Complete(_userSession.UserName ?? "System");
                    break;

                default:
                    return Result.Failure<OrderDto>($"Invalid state transition to {request.NewState}");
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Send push notification to customer based on state change
            await SendOrderStateChangeNotification(order, request.NewState, cancellationToken);

            // Send admin notification via SignalR
            await SendAdminNotificationForStateChange(order, request.NewState);

            // Return updated order DTO
            var orderDto = new OrderDto
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer.FullName,
                SubCategoryId = order.SubCategoryId,
                SubCategoryName = order.SubCategory.Name,
                CityId = order.CityId,
                CityName = order.City.Name,
                ReservationDateFrom = order.ReservationDateFrom,
                ReservationDateTo = order.ReservationDateTo,
                VehiclesCount = order.VehiclesCount,
                OrderSubTotal = order.OrderSubTotal,
                OrderTotal = order.OrderTotal,
                PreviousDebt = order.PreviousDebt,
                MoneyRefunded = order.MoneyRefunded,
                Notes = order.Notes,
                HotelName = order.HotelName,
                HotelAddress = order.HotelAddress,
                HotelPhone = order.HotelPhone,
                IsUrgent = order.IsUrgent,
                PaymentMethod = (PaymentMethod)order.PaymentMethodId,
                OrderState = order.OrderState,
                CreatedDate = order.CreatedDate
            };

            return Result.Success(orderDto);
        }

        private async Task SendOrderStateChangeNotification(Domain.Models.Order order, OrderState newState, CancellationToken cancellationToken)
        {
            try
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerId == order.CustomerId, cancellationToken);

                if (customer == null)
                    return;

                var firebaseTokens = new List<string>();
                if (!string.IsNullOrWhiteSpace(customer.AndriodDevice))
                    firebaseTokens.Add(customer.AndriodDevice);
                if (!string.IsNullOrWhiteSpace(customer.IosDevice))
                    firebaseTokens.Add(customer.IosDevice);

                if (firebaseTokens.Count == 0)
                    return;

                string title;
                string body;
                NotificationType notificationType;

                switch (newState)
                {
                    case OrderState.Confirmed:
                        title = "Order Confirmed";
                        body = $"Your order #{order.OrderCode} has been confirmed. Vehicles have been assigned.";
                        notificationType = NotificationType.OrderConfirmed;
                        break;
                    case OrderState.OnWay:
                        title = "Order On The Way";
                        body = $"Your order #{order.OrderCode} is on the way to your location.";
                        notificationType = NotificationType.OrderOnWay;
                        break;
                    case OrderState.CustomerReceived:
                        title = "Order Received";
                        body = $"Your order #{order.OrderCode} has been delivered. Please confirm receipt.";
                        notificationType = NotificationType.OrderCustomerReceived;
                        break;
                    case OrderState.Completed:
                        title = "Order Completed";
                        body = $"Your order #{order.OrderCode} has been completed successfully. Thank you!";
                        notificationType = NotificationType.OrderCompleted;
                        break;
                    default:
                        return;
                }

                var notificationBody = new NotificationBodyForMultipleDevices
                {
                    Title = title,
                    Body = body,
                    FireBaseTokens = firebaseTokens,
                    PayLoad = new Dictionary<string, string>
                    {
                        { "orderId", order.OrderId.ToString() },
                        { "orderCode", order.OrderCode },
                        { "type", ((int)notificationType).ToString() },
                        { "action", "open_order_detail" }
                    }
                };

                await _notificationService.SendNotificationAsyncToMultipleDevices(notificationBody);
            }
            catch (Exception)
            {
                // Notification failures should not affect order state changes
            }
        }

        private async Task SendAdminNotificationForStateChange(Domain.Models.Order order, OrderState newState)
        {
            try
            {
                string title;
                string message;
                NotificationType notificationType;

                switch (newState)
                {
                    case OrderState.Confirmed:
                        title = "Order Confirmed";
                        message = $"Order #{order.OrderCode} has been confirmed and vehicles assigned";
                        notificationType = NotificationType.OrderConfirmed;
                        break;
                    case OrderState.OnWay:
                        title = "Order On The Way";
                        message = $"Order #{order.OrderCode} is now on the way to customer";
                        notificationType = NotificationType.OrderOnWay;
                        break;
                    case OrderState.CustomerReceived:
                        title = "Order Received by Customer";
                        message = $"Order #{order.OrderCode} has been received by customer";
                        notificationType = NotificationType.OrderCustomerReceived;
                        break;
                    case OrderState.Completed:
                        title = "Order Completed";
                        message = $"Order #{order.OrderCode} has been completed successfully";
                        notificationType = NotificationType.OrderCompleted;
                        break;
                    default:
                        return; // Don't send notification for other states
                }

                await _adminNotificationHubService.SendNotificationAsync(
                    title: title,
                    message: message,
                    notificationType: notificationType,
                    orderId: order.OrderId
                );
            }
            catch (Exception)
            {
                // Log error but don't fail the order state update
                // Notification failures should not affect order state changes
            }
        }
    }
}

