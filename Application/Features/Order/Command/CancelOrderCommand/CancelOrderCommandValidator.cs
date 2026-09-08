using CSharpFunctionalExtensions;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.CancelOrderCommand
{
    public class CancelOrderCommandValidator
    {
        private readonly DatabaseContext _context;

        public CancelOrderCommandValidator(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result> ValidateAsync(CancelOrderCommand request, CancellationToken cancellationToken)
        {
            if (request.OrderId <= 0)
            {
                return Result.Failure("Order ID is required");
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

