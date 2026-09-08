using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Customer.Command.DeleteCustomerCommand
{
    public record DeleteCustomerCommand : IRequest<Result<bool>>
    {
        public int CustomerId { get; set; }
    }

    public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, Result<bool>>
    {
        private readonly DatabaseContext _context;

        public DeleteCustomerCommandHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<bool>> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
        {
            var customer = await _context.Customers
                .AsTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<bool>("Customer not found");
            }

            _context.Customers.Remove(customer);

            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            if (!saveResult.IsSuccess)
            {
                return Result.Failure<bool>($"Failed to delete customer: {saveResult.ErrorMessage}");
            }

            return Result.Success(true);
        }
    }
}

