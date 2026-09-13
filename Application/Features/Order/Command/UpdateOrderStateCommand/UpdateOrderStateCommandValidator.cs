using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.UpdateOrderStateCommand
{
    public class UpdateOrderStateCommandValidator
    {
        private readonly DatabaseContext _context;

        public UpdateOrderStateCommandValidator(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result> ValidateAsync(UpdateOrderStateCommand request, CancellationToken cancellationToken)
        {
            if (request.OrderId <= 0)
            {
                return Result.Failure("Order ID is required");
            }

            if (!Enum.IsDefined(typeof(OrderState), request.NewState))
            {
                return Result.Failure("Invalid order state");
            }

            // Admin Confirm only — lifecycle states are driven per-vehicle.
            var validTransitions = new[] { OrderState.Confirmed };
            if (!validTransitions.Contains(request.NewState))
            {
                return Result.Failure("Invalid state transition. Use UpdateState only for Confirmed; vehicle lifecycle endpoints for delivery/completion.");
            }

            var orderExists = await _context.Orders
                .AnyAsync(o => o.OrderId == request.OrderId, cancellationToken);

            if (!orderExists)
            {
                return Result.Failure("Order not found");
            }

            return Result.Success();
        }
    }
}

