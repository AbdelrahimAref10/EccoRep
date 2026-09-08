using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Customer.Command.SaveFireBaseTokensForCustomerCommand
{
    public record SaveFireBaseTokensForCustomerCommand : IRequest<Result>
    {
        public string? AndroidDevice { get; set; }
        public string? IosDevice { get; set; }

        private class SaveFireBaseTokensForCustomerCommandHandler : IRequestHandler<SaveFireBaseTokensForCustomerCommand, Result>
        {
            private readonly DatabaseContext _context;
            private readonly IUserSession _userSession;

            public SaveFireBaseTokensForCustomerCommandHandler(DatabaseContext context, IUserSession userSession)
            {
                _context = context;
                _userSession = userSession;
            }

            public async Task<Result> Handle(SaveFireBaseTokensForCustomerCommand request, CancellationToken cancellationToken)
            {
                var userId = _userSession.UserId;

                if (userId <= 0)
                {
                    return Result.Failure("User not found or not authenticated");
                }

                var customer = await _context.Customers
                    .AsTracking()
                    .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

                if (customer == null)
                {
                    return Result.Failure("Customer not found");
                }

                customer.AddFireBaseDevices(request.AndroidDevice, request.IosDevice, _userSession.UserName ?? "System");

                var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
                return (Result)saveResult;
            }
        }
    }
}


