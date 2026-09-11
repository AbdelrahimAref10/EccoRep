using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.AcceptMerchantOrderCommand
{
    public record AcceptMerchantOrderCommand : IRequest<Result<bool>>
    {
        public int OrderId { get; set; }
    }

    public class AcceptMerchantOrderCommandHandler : IRequestHandler<AcceptMerchantOrderCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public AcceptMerchantOrderCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(AcceptMerchantOrderCommand request, CancellationToken cancellationToken)
        {
            var merchant = await _context.Merchants
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == _userSession.UserId, cancellationToken);

            if (merchant == null)
                return Result.Failure<bool>("Merchant profile not found for current user");

            var order = await _context.Orders
                .AsTracking()
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (order == null)
                return Result.Failure<bool>($"Order with ID {request.OrderId} not found");

            if (order.OrderState != OrderState.MerchantPending)
                return Result.Failure<bool>($"Cannot accept merchant invitation in {order.OrderState} state");

            var merchantOrders = await _context.MerchantOrders
                .AsTracking()
                .Where(mo => mo.OrderId == request.OrderId)
                .ToListAsync(cancellationToken);

            var invitation = merchantOrders.FirstOrDefault(mo => mo.MerchantId == merchant.MerchantId);
            if (invitation == null)
                return Result.Failure<bool>("No invitation found for this merchant on the order");

            var modifiedBy = _userSession.UserName ?? "System";

            try
            {
                invitation.Accept(modifiedBy);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Failure<bool>(ex.Message);
            }

            // All invitations must be Accepted (no Pending; Rejected means admin must reassign first).
            var allAccepted = merchantOrders.All(mo => mo.ResponseStatus == MerchantOrderResponseStatus.Accepted);
            if (allAccepted)
            {
                try
                {
                    order.MarkMerchantConfirmed(modifiedBy);
                }
                catch (InvalidOperationException ex)
                {
                    return Result.Failure<bool>(ex.Message);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(true);
        }
    }
}
