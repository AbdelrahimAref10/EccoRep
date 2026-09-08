using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Customer.Command.AdminCreateCustomerCommand
{
    public class AdminCreateCustomerCommandValidator
    {
        private readonly DatabaseContext _context;

        public AdminCreateCustomerCommandValidator(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result> ValidateAsync(AdminCreateCustomerCommand request, CancellationToken cancellationToken)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.MobileNumber))
            {
                return Result.Failure("Mobile number is required");
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return Result.Failure("Full name is required");
            }

            if (string.IsNullOrWhiteSpace(request.Gender))
            {
                return Result.Failure("Gender is required");
            }

            // Validate RegisterAs enum value
            if (!Enum.IsDefined(typeof(RegisterAs), request.RegisterAs))
            {
                return Result.Failure("Invalid RegisterAs value");
            }

            // Validate VerificationBy enum value
            if (!Enum.IsDefined(typeof(VerificationBy), request.VerificationBy))
            {
                return Result.Failure("Invalid VerificationBy value");
            }

            // If verification by email, email is required
            if (request.VerificationBy == (int)VerificationBy.Email && string.IsNullOrWhiteSpace(request.Email))
            {
                return Result.Failure("Email is required when verification is by email");
            }

            // Validate email format if provided
            if (!string.IsNullOrWhiteSpace(request.Email) && !IsValidEmail(request.Email))
            {
                return Result.Failure("Invalid email format");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return Result.Failure("Password is required");
            }

            if (request.Password.Length < 6)
            {
                return Result.Failure("Password must be at least 6 characters long");
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

            // Check if customer with this mobile number already exists
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.MobileNumber == request.MobileNumber, cancellationToken);

            if (existingCustomer != null)
            {
                return Result.Failure("Customer with this mobile number already exists");
            }

            // If RegisterAs is Institution (1), CommercialRegisterImage is required
            if (request.RegisterAs == (int)RegisterAs.Institution && string.IsNullOrWhiteSpace(request.CommercialRegisterImage))
            {
                return Result.Failure("Commercial Register Image is required when registering as an Institution");
            }

            // If RegisterAs is Individual (0), CommercialRegisterImage should be null
            if (request.RegisterAs == (int)RegisterAs.Individual && !string.IsNullOrWhiteSpace(request.CommercialRegisterImage))
            {
                // Set to null for Individual customers
                request.CommercialRegisterImage = null;
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

