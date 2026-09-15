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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.AdminUpdateOrderCommand
{
    public record AdminUpdateOrderCommand : IRequest<Result<OrderDto>>
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public int SubCategoryId { get; set; }
        public int CityId { get; set; }
        public DateTime ReservationDateFrom { get; set; }
        public DateTime ReservationDateTo { get; set; }
        public int VehiclesCount { get; set; }
        public string? Notes { get; set; }
        /// <summary>
        /// Optional. When empty/null the existing passport image is kept.
        /// </summary>
        public string? PassportImage { get; set; }
        public string HotelName { get; set; } = string.Empty;
        public string HotelAddress { get; set; } = string.Empty;
        public string? HotelPhone { get; set; }
        public bool IsUrgent { get; set; }
        public int PaymentMethodId { get; set; }
        public int DestinationZoneId { get; set; }
    }

    public class AdminUpdateOrderCommandHandler : IRequestHandler<AdminUpdateOrderCommand, Result<OrderDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly INotificationService _notificationService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IOrderRealtimeNotifier _realtime;

        public AdminUpdateOrderCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            INotificationService notificationService,
            IDateTimeProvider dateTimeProvider,
            IOrderRealtimeNotifier realtime)
        {
            _context = context;
            _userSession = userSession;
            _notificationService = notificationService;
            _dateTimeProvider = dateTimeProvider;
            _realtime = realtime;
        }

        public async Task<Result<OrderDto>> Handle(AdminUpdateOrderCommand request, CancellationToken cancellationToken)
        {
            // Admin-updated orders are always Cash (ignore client payment method)
            request.PaymentMethodId = (int)PaymentMethod.Cash;

            var validator = new AdminUpdateOrderCommandValidator(_context, _dateTimeProvider);
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<OrderDto>(validationResult.Error);
            }

            var order = await _context.Orders
                .AsTracking()
                .Include(o => o.Customer)
                .Include(o => o.OrderPayments)
                .Include(o => o.OrderVehicles)
                    .ThenInclude(ov => ov.Vehicle)
                        .ThenInclude(v => v.Merchant)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                return Result.Failure<OrderDto>($"Order with ID {request.OrderId} not found");
            }

            if (order.OrderState != OrderState.Pending)
            {
                return Result.Failure<OrderDto>("Only pending orders can be edited");
            }

            var isCancelled = await CancellationDebtHelper.IsOrderCancelledAsync(_context, order, cancellationToken);

            if (isCancelled)
            {
                return Result.Failure<OrderDto>("Cannot edit a cancelled order");
            }

            var orderPayment = order.OrderPayments.FirstOrDefault();
            if (orderPayment != null && (orderPayment.State == PaymentState.Paid || orderPayment.State == PaymentState.Refunded))
            {
                return Result.Failure<OrderDto>("Cannot edit an order that is already paid or refunded");
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<OrderDto>("Customer not found");
            }

            if (customer.CashBlock)
            {
                return Result.Failure<OrderDto>("Cannot update admin order. Cash payment is blocked for this customer.");
            }

            var subCategory = await _context.SubCategories
                .FirstOrDefaultAsync(sc => sc.SubCategoryId == request.SubCategoryId && sc.IsActive, cancellationToken);

            if (subCategory == null)
            {
                return Result.Failure<OrderDto>("SubCategory not found or inactive");
            }

            var city = await _context.Cities
                .Include(c => c.TieredDiscounts)
                .FirstOrDefaultAsync(c => c.CityId == request.CityId, cancellationToken);

            if (city == null)
            {
                return Result.Failure<OrderDto>("City not found");
            }

            var assignedVehicles = order.OrderVehicles.Select(ov => ov.Vehicle).ToList();
            if (assignedVehicles.Count == 0)
            {
                return Result.Failure<OrderDto>("Order has no assigned vehicles");
            }

            var assignedVehicleIds = assignedVehicles.Select(v => v.VehicleId).ToList();
            var from = request.ReservationDateFrom.Date;
            var to = request.ReservationDateTo.Date;

            var stillBookedOverlaps = await _context.ReservedVehiclesPerDays
                .AsNoTracking()
                .Include(rv => rv.Order)
                .Where(rv => assignedVehicleIds.Contains(rv.VehicleId)
                    && rv.OrderId != request.OrderId
                    && rv.State == ReservedVehicleState.StillBooked
                    && rv.Order.OrderState != OrderState.Completed && rv.Order.OrderState != OrderState.Cancelled
                    && rv.DateFrom <= to
                    && rv.DateTo >= from)
                .Select(rv => new ValueTuple<int, DateTime, DateTime>(rv.VehicleId, rv.DateFrom, rv.DateTo))
                .ToListAsync(cancellationToken);

            var availability = Domain.Models.Order.EnsureSelectedVehiclesAreAvailable(
                assignedVehicles,
                request.SubCategoryId,
                from,
                to,
                stillBookedOverlaps);

            if (availability.IsFailure)
            {
                return Result.Failure<OrderDto>(availability.Error);
            }

            var passportImage = string.IsNullOrWhiteSpace(request.PassportImage)
                ? order.PassportImage
                : request.PassportImage;

            if (string.IsNullOrWhiteSpace(passportImage))
            {
                return Result.Failure<OrderDto>("Passport image is required");
            }

            if (!await OrderZoneFeeHelper.ZoneBelongsToCityAsync(_context, request.CityId, request.DestinationZoneId, cancellationToken))
            {
                return Result.Failure<OrderDto>("Destination zone is required and must belong to the order city group");
            }

            var reservationDays = Domain.Models.Order.InclusiveReservationDays(from, to);

            var rates = await OrderZoneFeeHelper.LoadRatesForCityAsync(_context, request.CityId, cancellationToken);
            var deliveryFees = OrderZoneFeeHelper.SumForVehicles(assignedVehicles, request.DestinationZoneId, rates);

            var pricing = Domain.Models.Order.CalculatePricing(
                assignedVehicles.Select(v => v.Price).ToList(),
                city,
                request.IsUrgent,
                reservationDays,
                deliveryFees,
                order.PreviousDebt);

            try
            {
                var actor = _userSession.UserName ?? "Admin";

                order.Update(
                    request.CustomerId,
                    request.SubCategoryId,
                    request.CityId,
                    request.ReservationDateFrom,
                    request.ReservationDateTo,
                    request.DestinationZoneId,
                    pricing,
                    passportImage,
                    request.HotelName,
                    request.HotelAddress,
                    (int)PaymentMethod.Cash,
                    request.IsUrgent,
                    request.HotelPhone,
                    request.Notes,
                    actor
                );

                foreach (var ov in order.OrderVehicles)
                {
                    var fee = Domain.Models.Order.CalculateVehicleDeliveryFee(
                        ov.Vehicle.Merchant.ZoneId,
                        request.DestinationZoneId,
                        rates);
                    ov.SetDeliveryFee(fee, actor);
                }

                var orderTotals = await _context.OrderTotals
                    .AsTracking()
                    .FirstOrDefaultAsync(ot => ot.OrderId == order.OrderId, cancellationToken);

                if (orderTotals != null)
                    orderTotals.Apply(pricing);
                else
                {
                    orderTotals = Domain.Models.OrderTotals.FromPricing(order.OrderId, pricing);
                    await _context.OrderTotals.AddAsync(orderTotals, cancellationToken);
                }

                if (orderPayment != null)
                {
                    orderPayment.Update((int)PaymentMethod.Cash, pricing.Total, actor);
                }
                else
                {
                    orderPayment = Domain.Models.OrderPayment.Create(
                        order.OrderId,
                        (int)PaymentMethod.Cash,
                        pricing.Total,
                        actor
                    );
                    await _context.OrderPayments.AddAsync(orderPayment, cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);

                await SendOrderUpdatedNotification(customer, order, cancellationToken);

                await _realtime.NotifyAsync(
                    order.OrderId,
                    "Order updated",
                    $"Order #{order.OrderCode} was updated by admin.",
                    NotificationType.OrderUpdated,
                    cancellationToken: cancellationToken);

                return Result.Success(new OrderDto
                {
                    OrderId = order.OrderId,
                    OrderCode = order.OrderCode,
                    CustomerId = order.CustomerId,
                    CustomerName = customer.FullName,
                    SubCategoryId = order.SubCategoryId,
                    SubCategoryName = subCategory.Name,
                    CityId = order.CityId,
                    CityName = city.Name,
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
                    PaymentMethod = PaymentMethod.Cash,
                    OrderState = order.OrderState,
                    CreatedDate = order.CreatedDate
                });
            }
            catch (Exception ex)
            {
                return Result.Failure<OrderDto>($"Error updating order: {ex.Message}");
            }
        }

        private async Task SendOrderUpdatedNotification(Domain.Models.Customer customer, Domain.Models.Order order, CancellationToken cancellationToken)
        {
            try
            {
                var customerWithTokens = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId, cancellationToken);

                if (customerWithTokens == null)
                    return;

                var firebaseTokens = new List<string>();
                if (!string.IsNullOrWhiteSpace(customerWithTokens.AndriodDevice))
                    firebaseTokens.Add(customerWithTokens.AndriodDevice);
                if (!string.IsNullOrWhiteSpace(customerWithTokens.IosDevice))
                    firebaseTokens.Add(customerWithTokens.IosDevice);

                if (firebaseTokens.Count == 0)
                    return;

                var notificationBody = new NotificationBodyForMultipleDevices
                {
                    Title = "Order Updated",
                    Body = $"Your order #{order.OrderCode} has been updated.",
                    FireBaseTokens = firebaseTokens,
                    PayLoad = new Dictionary<string, string>
                    {
                        { "orderId", order.OrderId.ToString() },
                        { "orderCode", order.OrderCode },
                        { "type", ((int)NotificationType.OrderCreated).ToString() },
                        { "action", "open_order_detail" }
                    }
                };

                await _notificationService.SendNotificationAsyncToMultipleDevices(notificationBody);
            }
            catch (Exception)
            {
                // Notification failures should not affect order update
            }
        }
    }
}
