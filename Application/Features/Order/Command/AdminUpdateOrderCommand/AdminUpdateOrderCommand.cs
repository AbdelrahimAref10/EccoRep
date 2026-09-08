using Application.Features.Order.Common;
using Application.Features.Order.DTOs;
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
using System.Globalization;
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
    }

    public class AdminUpdateOrderCommandHandler : IRequestHandler<AdminUpdateOrderCommand, Result<OrderDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly INotificationService _notificationService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public AdminUpdateOrderCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            INotificationService notificationService,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _userSession = userSession;
            _notificationService = notificationService;
            _dateTimeProvider = dateTimeProvider;
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

            var availabilityResult = await ValidateVehicleAvailabilityAsync(
                request.SubCategoryId,
                request.ReservationDateFrom,
                request.ReservationDateTo,
                request.VehiclesCount,
                excludeOrderId: request.OrderId,
                cancellationToken);

            if (availabilityResult.IsFailure)
            {
                return Result.Failure<OrderDto>(availabilityResult.Error);
            }

            var passportImage = string.IsNullOrWhiteSpace(request.PassportImage)
                ? order.PassportImage
                : request.PassportImage;

            if (string.IsNullOrWhiteSpace(passportImage))
            {
                return Result.Failure<OrderDto>("Passport image is required");
            }

            var from = request.ReservationDateFrom.Date;
            var to = request.ReservationDateTo.Date;
            var reservationDays = Math.Max(1, (int)(to - from).TotalDays + 1);

            // Same pricing method used by CalculateTotals preview and create.
            // Keep previous debt already attached to this order.
            var pricing = Domain.Models.Order.CalculatePricing(
                subCategory.Price,
                request.VehiclesCount,
                city,
                request.IsUrgent,
                reservationDays,
                order.PreviousDebt);

            var finalTotal = pricing.Total;

            try
            {
                var actor = _userSession.UserName ?? "Admin";

                order.Update(
                    request.CustomerId,
                    request.SubCategoryId,
                    request.CityId,
                    request.ReservationDateFrom,
                    request.ReservationDateTo,
                    pricing.VehiclesCount,
                    pricing.SubTotal,
                    finalTotal,
                    passportImage,
                    request.HotelName,
                    request.HotelAddress,
                    (int)PaymentMethod.Cash,
                    request.IsUrgent,
                    request.HotelPhone,
                    request.Notes,
                    actor
                );

                var orderTotals = await _context.OrderTotals
                    .AsTracking()
                    .FirstOrDefaultAsync(ot => ot.OrderId == order.OrderId, cancellationToken);

                if (orderTotals != null)
                {
                    orderTotals.Update(
                        pricing.SubTotal,
                        pricing.ServiceFees,
                        pricing.DeliveryFees,
                        pricing.UrgentFees,
                        pricing.TieredDiscountAmount,
                        finalTotal
                    );
                }
                else
                {
                    orderTotals = Domain.Models.OrderTotals.Create(
                        order.OrderId,
                        pricing.SubTotal,
                        pricing.ServiceFees,
                        pricing.DeliveryFees,
                        pricing.UrgentFees,
                        pricing.TieredDiscountAmount,
                        finalTotal
                    );
                    await _context.OrderTotals.AddAsync(orderTotals, cancellationToken);
                }

                if (orderPayment != null)
                {
                    orderPayment.Update((int)PaymentMethod.Cash, finalTotal, actor);
                }
                else
                {
                    orderPayment = Domain.Models.OrderPayment.Create(
                        order.OrderId,
                        (int)PaymentMethod.Cash,
                        finalTotal,
                        actor
                    );
                    await _context.OrderPayments.AddAsync(orderPayment, cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);

                await SendOrderUpdatedNotification(customer, order, cancellationToken);

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

        private async Task<Result> ValidateVehicleAvailabilityAsync(
            int subCategoryId,
            DateTime reservationDateFrom,
            DateTime reservationDateTo,
            int vehiclesCount,
            int? excludeOrderId,
            CancellationToken cancellationToken)
        {
            var from = reservationDateFrom.Date;
            var to = reservationDateTo.Date;

            var bookableVehiclesCount = await _context.Vehicles
                .CountAsync(v => v.SubCategoryId == subCategoryId
                    && v.Status != VehicleStatus.UnderMaintenance, cancellationToken);

            var reservedQuery = _context.ReservedVehiclesPerDays
                .AsNoTracking()
                .Include(rv => rv.Order)
                .Where(rv => rv.SubCategoryId == subCategoryId
                    && rv.State == ReservedVehicleState.StillBooked
                    && rv.Order.OrderState != OrderState.Completed && rv.Order.OrderState != OrderState.Cancelled
                    && rv.DateFrom <= to
                    && rv.DateTo >= from);

            if (excludeOrderId.HasValue)
            {
                reservedQuery = reservedQuery.Where(rv => rv.OrderId != excludeOrderId.Value);
            }

            var stillBookedOverlaps = await reservedQuery
                .Select(rv => new ValueTuple<DateTime, DateTime>(rv.DateFrom, rv.DateTo))
                .ToListAsync(cancellationToken);

            return Domain.Models.Order.EnsureSubCategoryHasCapacity(
                vehiclesCount,
                bookableVehiclesCount,
                from,
                to,
                stillBookedOverlaps);
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
