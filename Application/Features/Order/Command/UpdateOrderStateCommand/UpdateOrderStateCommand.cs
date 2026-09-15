using Application.Features.Order.Common;
using Application.Features.Order.DTOs;
using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Domain.Models;
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
        /// <summary>Unused for Confirm (vehicles already on order). Kept for API compatibility.</summary>
        public List<int>? VehicleIds { get; set; }
    }

    public class UpdateOrderStateCommandHandler : IRequestHandler<UpdateOrderStateCommand, Result<OrderDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly INotificationService _notificationService;
        private readonly IOrderRealtimeNotifier _realtime;

        public UpdateOrderStateCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            INotificationService notificationService,
            IOrderRealtimeNotifier realtime)
        {
            _context = context;
            _userSession = userSession;
            _notificationService = notificationService;
            _realtime = realtime;
        }

        public async Task<Result<OrderDto>> Handle(UpdateOrderStateCommand request, CancellationToken cancellationToken)
        {
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

            var actor = _userSession.UserName ?? "System";

            switch (request.NewState)
            {
                case OrderState.Confirmed:
                {
                    if (order.OrderState != OrderState.MerchantConfirmed)
                    {
                        return Result.Failure<OrderDto>($"Cannot confirm order. Current state: {order.OrderState}. Merchants must all accept first.");
                    }

                    if (order.OrderVehicles == null || order.OrderVehicles.Count == 0)
                    {
                        return Result.Failure<OrderDto>("Order has no vehicles assigned. Vehicles must be selected at create.");
                    }

                    if (order.OrderVehicles.Count != order.VehiclesCount)
                    {
                        return Result.Failure<OrderDto>($"Order vehicles mismatch. Expected {order.VehiclesCount}, found {order.OrderVehicles.Count}");
                    }

                    if (order.OrderVehicles.Any(ov => ov.MerchantResponseStatus != MerchantVehicleResponseStatus.Confirmed))
                    {
                        return Result.Failure<OrderDto>("Cannot confirm order while some vehicles are still pending or declined by merchants. Replace or remove declined vehicles first.");
                    }

                    var buildResult = await BuildMerchantPaymentDetailsAsync(order, actor, cancellationToken);
                    if (buildResult.IsFailure)
                        return Result.Failure<OrderDto>(buildResult.Error);

                    order.Confirm(actor);
                    break;
                }

                case OrderState.OnWay:
                case OrderState.CustomerReceived:
                case OrderState.Completed:
                    return Result.Failure<OrderDto>(
                        $"State {request.NewState} is driven by per-vehicle lifecycle. Use vehicle receive/deliver endpoints instead of UpdateState.");

                default:
                    return Result.Failure<OrderDto>($"Invalid state transition to {request.NewState}");
            }

            await _context.SaveChangesAsync(cancellationToken);

            await SendOrderStateChangeNotification(order, request.NewState, cancellationToken);
            await SendAdminNotificationForStateChange(order, request.NewState);

            return Result.Success(new OrderDto
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer.FullName,
                SubCategoryId = order.SubCategoryId,
                SubCategoryName = order.SubCategory.Name,
                CityId = order.CityId,
                CityName = order.City.Name,
                DestinationZoneId = order.DestinationZoneId,
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
            });
        }

        private async Task<Result> BuildMerchantPaymentDetailsAsync(
            Domain.Models.Order order,
            string actor,
            CancellationToken cancellationToken)
        {
            var existing = await _context.MerchantOrderPaymentDetails
                .AsNoTracking()
                .AnyAsync(p => p.OrderId == order.OrderId, cancellationToken);

            if (existing)
                return Result.Success();

            if (order.VehiclesCount <= 0)
                return Result.Failure("Invalid vehicles count");

            foreach (var ov in order.OrderVehicles)
            {
                var vehicle = ov.Vehicle;
                if (vehicle.MerchantId <= 0)
                    return Result.Failure($"Vehicle {vehicle.VehicleCode} has no merchant assigned");

                await _context.MerchantOrderPaymentDetails.AddAsync(
                    MerchantOrderPaymentDetail.Create(
                        order.OrderId,
                        vehicle.MerchantId,
                        vehicle.VehicleId,
                        order.CalculateVehicleRental(vehicle.Price),
                        actor),
                    cancellationToken);
            }

            return Result.Success();
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
                        body = $"Your order #{order.OrderCode} has been confirmed.";
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

                await _notificationService.SendNotificationAsyncToMultipleDevices(new NotificationBodyForMultipleDevices
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
                });
            }
            catch
            {
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
                        message = $"Order #{order.OrderCode} has been confirmed";
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
                        return;
                }

                await _realtime.NotifyAsync(
                    order.OrderId,
                    title,
                    message,
                    notificationType,
                    cancellationToken: default);
            }
            catch
            {
            }
        }
    }
}
