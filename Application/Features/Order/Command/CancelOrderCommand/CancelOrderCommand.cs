using Application.Features.Order.Common;
using Application.Features.Order.DTOs;
using Application.Features.Order.Services;
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

namespace Application.Features.Order.Command.CancelOrderCommand
{
    public record CancelOrderCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
    }

    public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly INotificationService _notificationService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IOrderRealtimeNotifier _realtime;

        public CancelOrderCommandHandler(
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

        public async Task<Result<bool>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders
                .AsTracking()
                .Include(o => o.Customer)
                .Include(o => o.City)
                .Include(o => o.OrderPayments)
                .Include(o => o.OrderVehicles)
                    .ThenInclude(ov => ov.Vehicle)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                return Result.Failure<bool>($"Order with ID {request.OrderId} not found");
            }

            if (order.OrderState == OrderState.Cancelled
                || await CancellationDebtHelper.IsOrderCancelledAsync(_context, order, cancellationToken))
            {
                return Result.Failure<bool>("Order is already cancelled");
            }

            if (!order.CanCancel())
            {
                return Result.Failure<bool>(
                    "Cannot cancel the order after a vehicle has been received from the merchant.");
            }

            var userId = _userSession.UserId;
            var isAdmin = _userSession.Roles.Contains(Domain.Enums.AppRoleNames.SuperAdmin);

            if (isAdmin)
            {
                return Result.Failure<bool>("Admin must reject the order; customer cancellation fees do not apply to admin.");
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

            if (customer == null || order.CustomerId != customer.CustomerId)
            {
                return Result.Failure<bool>("You do not have permission to cancel this order");
            }

            // Prior debt attached but not yet paid → release back to Pending
            await CancellationDebtHelper.RevertUnderPaymentFeesToPendingAsync(_context, order.OrderId, cancellationToken);

            // Fee is on rental portion only (excludes previous debt)
            var rentalTotal = order.GetRentalTotal();
            var cancellationFee = OrderCalculationService.CalculateCancellationFee(
                order.City,
                order.CreatedDate,
                rentalTotal,
                _dateTimeProvider);

            if (cancellationFee.HasValue && cancellationFee.Value > 0)
            {
                var walletEntry = CustomerWallet.Create(
                    order.CustomerId,
                    withdraw: cancellationFee.Value,
                    deposit: 0,
                    description: $"Order cancellation fee - Order #{order.OrderCode}",
                    type: WalletType.OrderCancellationFees,
                    orderId: order.OrderId
                );

                _context.CustomerWallets.Add(walletEntry);
            }

            var orderPayment = order.OrderPayments.FirstOrDefault();
            if (orderPayment != null && orderPayment.PaymentMethodId == (int)PaymentMethod.PayPal)
            {
                var refundableAmount = order.OrderTotal - order.PreviousDebt - (cancellationFee ?? 0);

                if (refundableAmount > 0)
                {
                    var refundablePaypal = Domain.Models.RefundablePaypalAmount.Create(
                        order.CustomerId,
                        order.OrderId,
                        order.OrderTotal,
                        cancellationFee ?? 0,
                        _userSession.UserName ?? "System",
                        order.PreviousDebt
                    );

                    _context.RefundablePaypalAmounts.Add(refundablePaypal);
                }

                orderPayment.MarkAsRefunded(_userSession.UserName ?? "System");
            }

            if (order.OrderState == OrderState.Confirmed
                || order.OrderState == OrderState.OnWay
                || order.OrderState == OrderState.CustomerReceived)
            {
                foreach (var orderVehicle in order.OrderVehicles)
                {
                    orderVehicle.Vehicle.UpdateStatus(VehicleStatus.Available, _userSession.UserName ?? "System");
                }
            }

            var reservedVehicles = await _context.ReservedVehiclesPerDays
                .AsTracking()
                .Where(rv => rv.OrderId == order.OrderId)
                .ToListAsync(cancellationToken);

            foreach (var reserved in reservedVehicles)
            {
                reserved.Cancel(_userSession.UserName ?? "System");
            }

            // Cash → MoneyRefunded=true; PayPal → false until admin confirms
            order.Cancel(_userSession.UserName ?? "System");

            await _context.SaveChangesAsync(cancellationToken);

            await SendOrderCancelledNotification(order, cancellationToken);

            await _realtime.NotifyAsync(
                order.OrderId,
                "Order Cancelled",
                $"Order #{order.OrderCode} has been cancelled",
                NotificationType.OrderCancelled,
                cancellationToken: cancellationToken);

            return Result.Success(true);
        }

        private async Task SendOrderCancelledNotification(Domain.Models.Order order, CancellationToken cancellationToken)
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

                var notificationBody = new NotificationBodyForMultipleDevices
                {
                    Title = "Order Cancelled",
                    Body = $"Your order #{order.OrderCode} has been cancelled.",
                    FireBaseTokens = firebaseTokens,
                    PayLoad = new Dictionary<string, string>
                    {
                        { "orderId", order.OrderId.ToString() },
                        { "orderCode", order.OrderCode },
                        { "type", ((int)NotificationType.OrderCancelled).ToString() },
                        { "action", "open_order_detail" }
                    }
                };

                await _notificationService.SendNotificationAsyncToMultipleDevices(notificationBody);
            }
            catch (Exception)
            {
                // Notification failures should not affect order cancellation
            }
        }
    }
}
