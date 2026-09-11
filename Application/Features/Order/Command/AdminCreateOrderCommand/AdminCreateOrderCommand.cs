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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.AdminCreateOrderCommand
{
    public record AdminCreateOrderCommand : IRequest<Result<OrderDto>>
    {
        public int CustomerId { get; set; }
        public int SubCategoryId { get; set; }
        public int CityId { get; set; }
        public DateTime ReservationDateFrom { get; set; }
        public DateTime ReservationDateTo { get; set; }
        public List<int> VehicleIds { get; set; } = new();
        public string? Notes { get; set; }
        public string PassportImage { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public string HotelAddress { get; set; } = string.Empty;
        public string? HotelPhone { get; set; }
        public bool IsUrgent { get; set; }
    }

    public class AdminCreateOrderCommandHandler : IRequestHandler<AdminCreateOrderCommand, Result<OrderDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly INotificationService _notificationService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IAdminNotificationHubService _adminNotificationHubService;

        public AdminCreateOrderCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            INotificationService notificationService,
            IDateTimeProvider dateTimeProvider,
            IAdminNotificationHubService adminNotificationHubService)
        {
            _context = context;
            _userSession = userSession;
            _notificationService = notificationService;
            _dateTimeProvider = dateTimeProvider;
            _adminNotificationHubService = adminNotificationHubService;
        }

        public async Task<Result<OrderDto>> Handle(AdminCreateOrderCommand request, CancellationToken cancellationToken)
        {
            var validator = new AdminCreateOrderCommandValidator(_context, _dateTimeProvider);
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<OrderDto>(validationResult.Error);
            }

            var vehicleIds = request.VehicleIds.Distinct().ToList();

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<OrderDto>("Customer not found");
            }

            if (customer.CashBlock)
            {
                return Result.Failure<OrderDto>("Cannot create admin order. Cash payment is blocked for this customer.");
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

            var vehicles = await _context.Vehicles
                .AsTracking()
                .Where(v => vehicleIds.Contains(v.VehicleId))
                .ToListAsync(cancellationToken);

            if (vehicles.Count != vehicleIds.Count)
            {
                return Result.Failure<OrderDto>("One or more vehicles not found");
            }

            var from = request.ReservationDateFrom.Date;
            var to = request.ReservationDateTo.Date;

            var stillBookedOverlaps = await _context.ReservedVehiclesPerDays
                .AsNoTracking()
                .Include(rv => rv.Order)
                .Where(rv => vehicleIds.Contains(rv.VehicleId)
                    && rv.State == ReservedVehicleState.StillBooked
                    && rv.Order.OrderState != OrderState.Completed && rv.Order.OrderState != OrderState.Cancelled
                    && rv.DateFrom <= to
                    && rv.DateTo >= from)
                .Select(rv => new ValueTuple<int, DateTime, DateTime>(rv.VehicleId, rv.DateFrom, rv.DateTo))
                .ToListAsync(cancellationToken);

            var availability = Domain.Models.Order.EnsureSelectedVehiclesAreAvailable(
                vehicles,
                request.SubCategoryId,
                from,
                to,
                stillBookedOverlaps);

            if (availability.IsFailure)
            {
                return Result.Failure<OrderDto>(availability.Error);
            }

            var reservationDays = Math.Max(1, (int)(to - from).TotalDays + 1);

            var pendingCancellationFees = await CancellationDebtHelper.GetPendingCancellationFeesAsync(
                _context,
                request.CustomerId,
                cancellationToken);
            var previousDebt = CancellationDebtHelper.SumWithdraw(pendingCancellationFees);

            var pricing = Domain.Models.Order.CalculatePricing(
                subCategory.Price,
                vehicleIds.Count,
                city,
                request.IsUrgent,
                reservationDays,
                previousDebt);

            var finalTotal = pricing.Total;

            var orderCode = GenerateOrderCode();
            var maxRetries = 10;
            var retryCount = 0;

            while (await _context.Orders.AnyAsync(o => o.OrderCode == orderCode, cancellationToken) && retryCount < maxRetries)
            {
                orderCode = GenerateOrderCode();
                retryCount++;
            }

            if (retryCount >= maxRetries)
            {
                return Result.Failure<OrderDto>("Failed to generate unique order code. Please try again.");
            }

            try
            {
                var actor = _userSession.UserName ?? "Admin";

                var order = Domain.Models.Order.Create(
                    request.CustomerId,
                    request.SubCategoryId,
                    request.CityId,
                    request.ReservationDateFrom,
                    request.ReservationDateTo,
                    pricing.VehiclesCount,
                    pricing.SubTotal,
                    finalTotal,
                    request.PassportImage,
                    request.HotelName,
                    request.HotelAddress,
                    (int)PaymentMethod.Cash,
                    request.IsUrgent,
                    orderCode,
                    request.HotelPhone,
                    request.Notes,
                    actor,
                    previousDebt
                );

                await _context.Orders.AddAsync(order, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                CancellationDebtHelper.AttachPendingFeesToOrder(pendingCancellationFees, order.OrderId);

                var orderTotals = Domain.Models.OrderTotals.Create(
                    order.OrderId,
                    pricing.SubTotal,
                    pricing.ServiceFees,
                    pricing.DeliveryFees,
                    pricing.UrgentFees,
                    pricing.TieredDiscountAmount,
                    finalTotal
                );

                var orderPayment = Domain.Models.OrderPayment.Create(
                    order.OrderId,
                    (int)PaymentMethod.Cash,
                    finalTotal,
                    actor
                );

                await _context.OrderTotals.AddAsync(orderTotals, cancellationToken);
                await _context.OrderPayments.AddAsync(orderPayment, cancellationToken);

                // Vehicles at create — stay Pending (no auto-confirm)
                foreach (var vehicleId in vehicleIds)
                {
                    _context.OrderVehicles.Add(Domain.Models.OrderVehicle.Create(order.OrderId, vehicleId, actor));
                }

                foreach (var vehicle in vehicles)
                {
                    var currentDate = order.ReservationDateFrom.Date;
                    var endDate = order.ReservationDateTo.Date;

                    while (currentDate <= endDate)
                    {
                        _context.ReservedVehiclesPerDays.Add(Domain.Models.ReservedVehiclesPerDays.Create(
                            vehicle.VehicleId,
                            order.SubCategoryId,
                            vehicle.VehicleCode,
                            order.OrderId,
                            currentDate,
                            currentDate,
                            actor
                        ));
                        currentDate = currentDate.AddDays(1);
                    }

                    vehicle.UpdateStatus(VehicleStatus.Rented, actor);
                }

                await _context.SaveChangesAsync(cancellationToken);

                await SendOrderCreatedNotification(customer, order, cancellationToken);

                await _adminNotificationHubService.SendNotificationAsync(
                    title: "New Order Created",
                    message: $"New order #{order.OrderCode} has been created by admin for customer {customer.FullName}",
                    notificationType: NotificationType.OrderCreated,
                    orderId: order.OrderId
                );

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
                return Result.Failure<OrderDto>($"Error creating order: {ex.Message}");
            }
        }

        private string GenerateOrderCode()
        {
            var datePart = _dateTimeProvider.Now.ToString("yyyyMMdd");
            var randomPart = GenerateRandomAlphanumeric(6);
            return $"ORD-{datePart}-{randomPart}";
        }

        private static string GenerateRandomAlphanumeric(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async Task SendOrderCreatedNotification(Domain.Models.Customer customer, Domain.Models.Order order, CancellationToken cancellationToken)
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
                    Title = "Order Created",
                    Body = $"Your order #{order.OrderCode} has been created and is pending.",
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
            }
        }
    }
}
