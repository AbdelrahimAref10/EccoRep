using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.MarkOrderMoneyRefundedCommand
{
    /// <summary>
    /// Admin confirms PayPal refund was completed ("تم الرد" / Mark as refunded).
    /// Sets <c>MoneyRefunded = true</c> and marks pending <c>RefundablePaypalAmount</c> as Success.
    /// </summary>
    public record MarkOrderMoneyRefundedCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
    }

    public class MarkOrderMoneyRefundedCommandHandler : IRequestHandler<MarkOrderMoneyRefundedCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public MarkOrderMoneyRefundedCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(MarkOrderMoneyRefundedCommand request, CancellationToken cancellationToken)
        {
            var order = await _context.Orders
                .AsTracking()
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
            {
                return Result.Failure<bool>($"Order with ID {request.OrderId} not found");
            }

            if (order.MoneyRefunded)
            {
                return Result.Failure<bool>("Money is already marked as refunded for this order");
            }

            if (order.PaymentMethodId != (int)PaymentMethod.PayPal)
            {
                return Result.Failure<bool>("Only PayPal orders require an admin refund confirmation");
            }

            var refundable = await _context.RefundablePaypalAmounts
                .AsTracking()
                .FirstOrDefaultAsync(r => r.OrderId == order.OrderId, cancellationToken);

            if (refundable == null)
            {
                return Result.Failure<bool>("No refundable PayPal amount found for this order");
            }

            if (refundable.State == RefundState.Success)
            {
                order.MarkMoneyRefunded(_userSession.UserName ?? "Admin");
                await _context.SaveChangesAsync(cancellationToken);
                return Result.Success(true);
            }

            if (refundable.State == RefundState.Failed)
            {
                return Result.Failure<bool>("Refundable PayPal amount is marked as Failed");
            }

            refundable.MarkAsSuccess(_userSession.UserName ?? "Admin");
            order.MarkMoneyRefunded(_userSession.UserName ?? "Admin");

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(true);
        }
    }
}
