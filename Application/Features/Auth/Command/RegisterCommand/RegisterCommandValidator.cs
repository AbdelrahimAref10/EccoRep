using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.Command.RegisterCommand
{
    public class RegisterCommandValidator
    {
        private readonly DatabaseContext _context;

        public RegisterCommandValidator(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result> ValidateAsync(RegisterCommand request, CancellationToken cancellationToken)
        {
            if (!AppRoleNames.TryFromInt(request.Role, out var appRole))
                return Result.Failure("Invalid role");

            if (!AppRoleNames.IsPublicRegistrationAllowed(appRole))
                return Result.Failure("Super Admin accounts cannot be registered publicly");

            if (string.IsNullOrWhiteSpace(request.MobileNumber))
                return Result.Failure("Mobile number is required");

            if (string.IsNullOrWhiteSpace(request.FullName))
                return Result.Failure("Full name is required");

            if (string.IsNullOrWhiteSpace(request.Password))
                return Result.Failure("Password is required");

            if (request.Password.Length < 6)
                return Result.Failure("Password must be at least 6 characters long");

            if (!string.IsNullOrWhiteSpace(request.Email) && !IsValidEmail(request.Email))
                return Result.Failure("Invalid email format");

            if (appRole == AppRole.Customer)
            {
                if (string.IsNullOrWhiteSpace(request.Gender))
                    return Result.Failure("Gender is required");

                if (request.Gender != "Male" && request.Gender != "Female")
                    return Result.Failure("Gender must be either 'Male' or 'Female'");

                if (!request.CityId.HasValue || request.CityId <= 0)
                    return Result.Failure("Valid city is required");

                if (!request.RegisterAs.HasValue || !Enum.IsDefined(typeof(RegisterAs), request.RegisterAs.Value))
                    return Result.Failure("Invalid RegisterAs value");

                if (!request.VerificationBy.HasValue || !Enum.IsDefined(typeof(VerificationBy), request.VerificationBy.Value))
                    return Result.Failure("Invalid VerificationBy value");

                if (request.VerificationBy == (int)VerificationBy.Email && string.IsNullOrWhiteSpace(request.Email))
                    return Result.Failure("Email is required when verification is by email");

                var cityExists = await _context.Cities.AnyAsync(c => c.CityId == request.CityId && c.IsActive, cancellationToken);
                if (!cityExists)
                    return Result.Failure("Invalid or inactive city");

                if (request.RegisterAs == (int)RegisterAs.Institution && string.IsNullOrWhiteSpace(request.CommercialRegisterImage))
                    return Result.Failure("Commercial Register Image is required when registering as an Institution");

                if (request.RegisterAs == (int)RegisterAs.Individual)
                    request.CommercialRegisterImage = null;

                var existingCustomer = await _context.Customers
                    .AnyAsync(c => c.MobileNumber == request.MobileNumber, cancellationToken);
                if (existingCustomer)
                    return Result.Failure("Customer with this mobile number already exists");
            }

            if (appRole == AppRole.Merchant || appRole == AppRole.Delivery)
            {
                if (!request.CityId.HasValue || request.CityId <= 0)
                    return Result.Failure("Valid city is required");

                var cityExists = await _context.Cities.AnyAsync(c => c.CityId == request.CityId && c.IsActive, cancellationToken);
                if (!cityExists)
                    return Result.Failure("Invalid or inactive city");
            }

            if (appRole == AppRole.Merchant)
            {
                var exists = await _context.Merchants.AnyAsync(m => m.MobileNumber == request.MobileNumber && !m.IsDeleted, cancellationToken);
                if (exists)
                    return Result.Failure("Merchant with this mobile number already exists");
            }

            if (appRole == AppRole.Delivery)
            {
                var exists = await _context.Deliveries.AnyAsync(d => d.MobileNumber == request.MobileNumber && !d.IsDeleted, cancellationToken);
                if (exists)
                    return Result.Failure("Delivery with this mobile number already exists");
            }

            return Result.Success();
        }

        private static bool IsValidEmail(string email)
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
