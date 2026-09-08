using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.AdminCreateOrderCommand
{
    public class AdminCreateOrderCommandValidator
    {
        private readonly DatabaseContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public AdminCreateOrderCommandValidator(DatabaseContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result> ValidateAsync(AdminCreateOrderCommand request, CancellationToken cancellationToken)
        {
            if (request.CustomerId <= 0)
            {
                return Result.Failure("Customer ID is required");
            }

            var customerExists = await _context.Customers
                .AnyAsync(c => c.CustomerId == request.CustomerId, cancellationToken);
            if (!customerExists)
            {
                return Result.Failure("Customer not found");
            }

            if (request.SubCategoryId <= 0)
            {
                return Result.Failure("SubCategory ID is required");
            }

            var subCategoryExists = await _context.SubCategories
                .AnyAsync(sc => sc.SubCategoryId == request.SubCategoryId && sc.IsActive, cancellationToken);
            if (!subCategoryExists)
            {
                return Result.Failure("SubCategory not found or inactive");
            }

            if (request.CityId <= 0)
            {
                return Result.Failure("City ID is required");
            }

            var cityExists = await _context.Cities
                .AnyAsync(c => c.CityId == request.CityId, cancellationToken);
            if (!cityExists)
            {
                return Result.Failure("City not found");
            }

            if (request.ReservationDateFrom < _dateTimeProvider.Now.Date)
            {
                return Result.Failure("Reservation date from must be a future date");
            }

            if (request.ReservationDateTo < request.ReservationDateFrom)
            {
                return Result.Failure("Reservation date to must be on or after reservation date from");
            }

            if (request.VehicleIds == null || request.VehicleIds.Count == 0)
            {
                return Result.Failure("At least one vehicle must be selected");
            }

            if (request.VehicleIds.Distinct().Count() != request.VehicleIds.Count)
            {
                return Result.Failure("Duplicate vehicle IDs are not allowed");
            }

            if (string.IsNullOrWhiteSpace(request.PassportImage))
            {
                return Result.Failure("Passport image is required");
            }

            if (string.IsNullOrWhiteSpace(request.HotelName))
            {
                return Result.Failure("Hotel name is required");
            }

            if (string.IsNullOrWhiteSpace(request.HotelAddress))
            {
                return Result.Failure("Hotel address is required");
            }

            if (request.HotelAddress.Length > 500)
            {
                return Result.Failure("Hotel address must not exceed 500 characters");
            }

            return Result.Success();
        }
    }
}
