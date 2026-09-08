using CSharpFunctionalExtensions;
using Domain.Common;
using Domain.Models;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Customer.Command.SaveCustomerLocationCommand
{
    public record SaveCustomerLocationCommand : IRequest<Result>
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }

    public class SaveCustomerLocationCommandHandler : IRequestHandler<SaveCustomerLocationCommand, Result>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;
        private readonly IDateTimeProvider _dateTimeProvider;

        public SaveCustomerLocationCommandHandler(
            DatabaseContext context,
            IUserSession userSession,
            IDateTimeProvider dateTimeProvider)
        {
            _context = context;
            _userSession = userSession;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<Result> Handle(SaveCustomerLocationCommand request, CancellationToken cancellationToken)
        {
            // Validate coordinates
            if (request.Longitude < -180 || request.Longitude > 180)
            {
                return Result.Failure("Invalid longitude value. Must be between -180 and 180.");
            }

            if (request.Latitude < -90 || request.Latitude > 90)
            {
                return Result.Failure("Invalid latitude value. Must be between -90 and 90.");
            }

            var userId = _userSession.UserId;

            if (userId <= 0)
            {
                return Result.Failure("User not found or not authenticated");
            }

            // Get customer with location (if exists)
            var customer = await _context.Customers
                .Include(c => c.CustomerLocation)
                .AsTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure("Customer not found");
            }

            // Save or update location using Egypt local time
            customer.SaveLocation(request.Longitude, request.Latitude, _dateTimeProvider.Now);

            // Save changes to database
            var saveResult = await _context.SaveChangesAsyncWithResult(cancellationToken);
            return (Result)saveResult;
        }
    }
}

