using Application.Features.Order.Command.PayPalPaymentCommands.CompletePayPalPaymentCommand.DTOs;
using Application.Features.Order.Common;
using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Domain.Services;
using Infrastructure;
using Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.PayPalPaymentCommands.CompletePayPalPaymentCommand
{
    public record CompletePayPalPaymentCommand : IRequest<Result<CompletePayPalPaymentResponseDto>>
    {
        public int OrderId { get; set; }
        public string PayPalOrderId { get; set; } = string.Empty;
    }

    public class CompletePayPalPaymentCommandHandler : IRequestHandler<CompletePayPalPaymentCommand, Result<CompletePayPalPaymentResponseDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IPayPalService _payPalService;
        private readonly IOrderRealtimeNotifier _realtime;

        public CompletePayPalPaymentCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IPayPalService payPalService,
            IOrderRealtimeNotifier realtime)
        {
            _context = context;
            _userSession = userSession;
            _payPalService = payPalService;
            _realtime = realtime;
        }

        public async Task<Result<CompletePayPalPaymentResponseDto>> Handle(
            CompletePayPalPaymentCommand request,
            CancellationToken cancellationToken)
        {
            // Validate PayPal Order ID
            if (string.IsNullOrWhiteSpace(request.PayPalOrderId))
            {
                return Result.Failure<CompletePayPalPaymentResponseDto>("PayPal Order ID is required");
            }

            if (_userSession.UserId <= 0)
            {
                return Result.Failure<CompletePayPalPaymentResponseDto>("Customer not authenticated");
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == _userSession.UserId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<CompletePayPalPaymentResponseDto>("Customer not found");
            }

            // Verify the order belongs to the current customer
            var order = await _context.Orders
                .AsTracking()
                .Include(o => o.OrderPayments)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId && o.CustomerId == customer.CustomerId, cancellationToken);

            if (order == null)
            {
                return Result.Failure<CompletePayPalPaymentResponseDto>("Order not found or access denied");
            }

            // Verify payment method is PayPal
            var orderPayment = order.OrderPayments.FirstOrDefault();
            if (orderPayment == null || orderPayment.PaymentMethodId != (int)PaymentMethod.PayPal)
            {
                return Result.Failure<CompletePayPalPaymentResponseDto>("Order payment method is not PayPal");
            }

            // Verify payment is still pending
            if (orderPayment.State != PaymentState.Pending)
            {
                return Result.Failure<CompletePayPalPaymentResponseDto>($"Payment is already {orderPayment.State}");
            }

            // Capture the PayPal payment
            var captureResult = await _payPalService.CapturePayPalOrderAsync(request.PayPalOrderId);

            if (captureResult.IsSuccess)
            {
                // Mark payment as paid
                orderPayment.MarkAsPaid(_userSession.UserName ?? "System");

                // Settle previous cancellation debt collected with this order
                await CancellationDebtHelper.MarkUnderPaymentFeesAsPaidAsync(
                    _context,
                    order.OrderId,
                    cancellationToken);

                // Create treasury record for successful PayPal payment
                var actor = _userSession.UserName ?? "System";
                var treasuryRecord = TreasuryService.CreatePayPalPaymentRecord(
                    orderPayment.Total,
                    order.OrderCode,
                    actor);
                _context.CompanyTreasuries.Add(treasuryRecord);

                try
                {
                    order.BuildOnlinePaymentCapturedLedgerLines(actor);
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
                {
                    return Result.Failure<CompletePayPalPaymentResponseDto>(ex.Message);
                }

                await _context.SaveChangesAsync(cancellationToken);

                await _realtime.NotifyAsync(
                    order.OrderId,
                    "PayPal payment completed",
                    $"PayPal payment captured for order #{order.OrderCode}.",
                    NotificationType.OrderUpdated,
                    cancellationToken: cancellationToken);

                return Result.Success(new CompletePayPalPaymentResponseDto
                {
                    IsSuccess = true,
                    TransactionId = captureResult.TransactionId,
                    Message = "Payment completed successfully"
                });
            }
            else
            {
                // Mark payment as failed
                orderPayment.MarkAsFailed(_userSession.UserName ?? "System");
                await _context.SaveChangesAsync(cancellationToken);

                return Result.Failure<CompletePayPalPaymentResponseDto>(
                    captureResult.ErrorMessage ?? "Failed to capture PayPal payment");
            }
        }
    }
}
