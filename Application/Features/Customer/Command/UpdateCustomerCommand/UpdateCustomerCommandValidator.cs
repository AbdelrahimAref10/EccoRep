using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Customer.Command.UpdateCustomerCommand
{
    public class UpdateCustomerCommandValidator
    {
        private readonly DatabaseContext _context;

        public UpdateCustomerCommandValidator(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result> ValidateAsync(UpdateCustomerCommand request, CancellationToken cancellationToken)
        {
            if (request.CustomerId <= 0)
            {
                return Result.Failure("Valid customer ID is required");
            }

            // Validate that customer exists
            var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == request.CustomerId, cancellationToken);
            if (!customerExists)
            {
                return Result.Failure("Customer not found");
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return Result.Failure("Full name is required");
            }

            if (string.IsNullOrWhiteSpace(request.Gender))
            {
                return Result.Failure("Gender is required");
            }

            if (request.CityId <= 0)
            {
                return Result.Failure("Valid city is required");
            }

            // Validate that City exists and is active
            var cityExists = await _context.Cities.AnyAsync(c => c.CityId == request.CityId && c.IsActive, cancellationToken);
            if (!cityExists)
            {
                return Result.Failure("Invalid or inactive city");
            }

            // Validate gender value
            if (request.Gender != "Male" && request.Gender != "Female")
            {
                return Result.Failure("Gender must be either 'Male' or 'Female'");
            }

            return Result.Success();
        }
    }
}

