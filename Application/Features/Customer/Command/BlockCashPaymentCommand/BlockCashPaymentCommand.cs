using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Customer.Command.BlockCashPaymentCommand
{
    public record BlockCashPaymentCommand : IRequest<Result<bool>>
    {
        public int CustomerId { get; set; }
    }

    public class BlockCashPaymentCommandHandler : IRequestHandler<BlockCashPaymentCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public BlockCashPaymentCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(BlockCashPaymentCommand request, CancellationToken cancellationToken)
        {
            var customer = await _context.Customers
                .AsTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<bool>("Customer not found");
            }

            customer.BlockCashPayment(_userSession.UserId.ToString());

            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<bool>($"Failed to block cash payment: {saveResult.ErrorMessage}");
            }

            return Result.Success(true);
        }
    }
}

