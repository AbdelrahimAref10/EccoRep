using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Enums;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Command.AdminUpdateOrderCommand
{
    public class AdminUpdateOrderCommandValidator
    {
        private readonly DatabaseContext _context;
        private readonly IDateTimeProvider _dateTimeProvider;

        public AdminUpdateOrderCommandValidator(DatabaseContext context, IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result> ValidateAsync(AdminUpdateOrderCommand request, CancellationToken cancellationToken)
        {
            if (request.OrderId <= 0)
            {
                return Result.Failure("Order ID is required");
            }

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

            if (request.DestinationZoneId <= 0)
                return Result.Failure("Destination zone is required");

            if (request.ReservationDateFrom < _dateTimeProvider.Now.Date)
            {
                return Result.Failure("Reservation date from must be a future date");
            }

            if (request.ReservationDateTo < request.ReservationDateFrom)
            {
                return Result.Failure("Reservation date to must be on or after reservation date from");
            }

            if (request.VehiclesCount <= 0)
            {
                return Result.Failure("Vehicles count must be greater than zero");
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

            // Admin orders are Cash-only
            if (request.PaymentMethodId != (int)PaymentMethod.Cash)
            {
                return Result.Failure("Admin orders support Cash payment only");
            }

            return Result.Success();
        }
    }
}
