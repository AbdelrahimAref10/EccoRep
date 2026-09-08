using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Customer.Command.DeactivateCustomerCommand
{
    public record DeactivateCustomerCommand : IRequest<Result<bool>>
    {
        public int CustomerId { get; set; }
    }

    public class DeactivateCustomerCommandHandler : IRequestHandler<DeactivateCustomerCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public DeactivateCustomerCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(DeactivateCustomerCommand request, CancellationToken cancellationToken)
        {
            var customer = await _context.Customers
                .AsTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<bool>("Customer not found");
            }

            customer.Deactivate(_userSession.UserId.ToString());

            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<bool>($"Failed to deactivate customer: {saveResult.ErrorMessage}");
            }

            return Result.Success(true);
        }
    }
}

