using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Customer.Command.UpdateCustomerProfileCommand
{
    public class UpdateCustomerProfileCommandValidator
    {
        private readonly DatabaseContext _context;

        public UpdateCustomerProfileCommandValidator(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result> ValidateAsync(UpdateCustomerProfileCommand request, int customerId, CancellationToken cancellationToken)
        {
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

            // Validate email format if provided
            if (!string.IsNullOrWhiteSpace(request.Email) && !IsValidEmail(request.Email))
            {
                return Result.Failure("Invalid email format");
            }

            // Validate password if provided (must be at least 8 characters)
            if (!string.IsNullOrWhiteSpace(request.Password) && request.Password.Length < 8)
            {
                return Result.Failure("Password must be at least 8 characters long");
            }

            // Validate that customer exists
            var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == customerId, cancellationToken);
            if (!customerExists)
            {
                return Result.Failure("Customer not found");
            }

            return Result.Success();
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}

