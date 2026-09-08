using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Customer.Command.UnblockCustomerCommand
{
    public record UnblockCustomerCommand : IRequest<Result<bool>>
    {
        public int CustomerId { get; set; }
    }

    public class UnblockCustomerCommandHandler : IRequestHandler<UnblockCustomerCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public UnblockCustomerCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(UnblockCustomerCommand request, CancellationToken cancellationToken)
        {
            var customer = await _context.Customers
                .AsTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<bool>("Customer not found");
            }

            customer.Unblock(_userSession.UserId.ToString());

            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<bool>($"Failed to unblock customer: {saveResult.ErrorMessage}");
            }

            return Result.Success(true);
        }
    }
}

