using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Customer.Command.AdminActivateCustomerCommand
{
    public record AdminActivateCustomerCommand : IRequest<Result<bool>>
    {
        public int CustomerId { get; set; }
    }

    public class AdminActivateCustomerCommandHandler : IRequestHandler<AdminActivateCustomerCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public AdminActivateCustomerCommandHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<bool>> Handle(AdminActivateCustomerCommand request, CancellationToken cancellationToken)
        {
            var customer = await _context.Customers
                .AsTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<bool>("Customer not found");
            }

            if (customer.State == Domain.Enums.CustomerState.Active)
            {
                return Result.Failure<bool>("Customer is already active");
            }

            customer.Activate(_userSession.UserId.ToString());

            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<bool>($"Failed to activate customer: {saveResult.ErrorMessage}");
            }

            return Result.Success(true);
        }
    }
}

