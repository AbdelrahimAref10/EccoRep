using Application.Features.Order.Services;
using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.RejectMerchantOrderCommand
{
    public record RejectMerchantOrderCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class RejectMerchantOrderCommandHandler : IRequestHandler<RejectMerchantOrderCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IOrderRealtimeNotifier _realtime;

        public RejectMerchantOrderCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IOrderRealtimeNotifier realtime)
        {
            _context = context;
            _userSession = userSession;
            _realtime = realtime;
        }

        public async Task<Result<bool>> Handle(RejectMerchantOrderCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return Result.Failure<bool>("Reject reason is required");

            var merchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId, cancellationToken);

            if (merchant == null)
                return Result.Failure<bool>("Merchant profile not found for current user");

            var order = await _context.Orders
                .AsTracking()
                .Include(o => o.OrderVehicles)
                    .ThenInclude(ov => ov.Vehicle)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
                return Result.Failure<bool>($"Order with ID {request.OrderId} not found");

            if (order.OrderState != OrderState.MerchantPending)
                return Result.Failure<bool>($"Cannot reject merchant invitation in {order.OrderState} state");

            var invitation = await _context.MerchantOrders
                .AsTracking()
                .FirstOrDefaultAsync(
                    mo => mo.OrderId == request.OrderId && mo.MerchantId == merchant.MerchantId,
                    cancellationToken);

            if (invitation == null)
                return Result.Failure<bool>("No invitation found for this merchant on the order");

            try
            {
                invitation.Reject(request.Reason, _userSession.UserName ?? "System");
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<bool>(ex.Message);
            }

            var actor = _userSession.UserName ?? "System";
            foreach (var ov in order.OrderVehicles.Where(v => v.Vehicle.MerchantId == merchant.MerchantId))
                ov.DeclineByMerchant(actor);

            // Order stays MerchantPending until admin reassigns rejected invitations.
            await _context.SaveChangesAsync(cancellationToken);

            await _realtime.NotifyAsync(
                order.OrderId,
                "Merchant rejected invitation",
                $"Merchant {merchant.FullName} rejected order #{order.OrderCode}.",
                NotificationType.OrderMerchantPending,
                new[] { merchant.MerchantId },
                cancellationToken: cancellationToken);

            return Result.Success(true);
        }
    }
}
