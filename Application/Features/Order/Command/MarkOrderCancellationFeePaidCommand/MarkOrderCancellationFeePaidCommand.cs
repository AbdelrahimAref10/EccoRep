using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.MarkOrderCancellationFeePaidCommand
{
    public record MarkOrderCancellationFeePaidCommand : IRequest<Result<int>>
    {
        public int OrderId { get; set; }
    }

    public class MarkOrderCancellationFeePaidCommandHandler : IRequestHandler<MarkOrderCancellationFeePaidCommand, Result<int>>
    {
        private readonly DatabaseContext _context;

        public MarkOrderCancellationFeePaidCommandHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<int>> Handle(MarkOrderCancellationFeePaidCommand request, CancellationToken cancellationToken)
        {
            var walletEntry = await _context.CustomerWallets
                .AsTracking()
                .FirstOrDefaultAsync(
                    cw => cw.OrderId == request.OrderId
                        && cw.Type == WalletType.OrderCancellationFees
                        && cw.State == CustomerWalletState.Pending,
                    cancellationToken);

            if (walletEntry == null)
            {
                return Result.Failure<int>("No pending cancellation fee found for this order.");
            }

            if (walletEntry.State == CustomerWalletState.Paid)
            {
                return Result.Failure<int>("Cancellation fee for this order is already marked as paid.");
            }

            walletEntry.MarkAsPaid();
            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<int>("Cannot mark order as paid");
            }

            return Result.Success<int>(request.OrderId);
        }
    }
}
