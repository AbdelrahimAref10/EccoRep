using Application.Features.Order.Common;
using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Query.GetCustomerOrderDeliveryFeesQuery
{
    public record CustomerOrderDeliveryFeesDto
    {
        public decimal DeliveryFees { get; set; }
    }

    public record GetCustomerOrderDeliveryFeesQuery : IRequest<Result<CustomerOrderDeliveryFeesDto>>
    {
        public int DestinationZoneId { get; set; }
        public List<int> VehicleIds { get; set; } = new();
    }

    public class GetCustomerOrderDeliveryFeesQueryHandler
        : IRequestHandler<GetCustomerOrderDeliveryFeesQuery, Result<CustomerOrderDeliveryFeesDto>>
    {
        private readonly DatabaseContext _context;
        private readonly IUserSession _userSession;

        public GetCustomerOrderDeliveryFeesQueryHandler(DatabaseContext context, IUserSession userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public async Task<Result<CustomerOrderDeliveryFeesDto>> Handle(
            GetCustomerOrderDeliveryFeesQuery request,
            CancellationToken cancellationToken)
        {
            if (request.DestinationZoneId <= 0)
                return Result.Failure<CustomerOrderDeliveryFeesDto>("Destination zone is required");

            if (request.VehicleIds == null || request.VehicleIds.Count == 0)
                return Result.Failure<CustomerOrderDeliveryFeesDto>("At least one vehicle is required");

            var customer = await _context.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == _userSession.UserId, cancellationToken);

            if (customer == null)
                return Result.Failure<CustomerOrderDeliveryFeesDto>("Customer not found");

            if (!await OrderZoneFeeHelper.ZoneBelongsToCityAsync(_context, customer.CityId, request.DestinationZoneId, cancellationToken))
                return Result.Failure<CustomerOrderDeliveryFeesDto>("Destination zone must belong to the customer city group");

            var vehicleIds = request.VehicleIds.Distinct().ToList();
            var vehicles = await _context.Vehicles
                .AsNoTracking()
                .Include(v => v.Merchant)
                .Where(v => vehicleIds.Contains(v.VehicleId))
                .ToListAsync(cancellationToken);

            if (vehicles.Count != vehicleIds.Count)
                return Result.Failure<CustomerOrderDeliveryFeesDto>("One or more vehicles not found");

            var rates = await OrderZoneFeeHelper.LoadRatesForCityAsync(_context, customer.CityId, cancellationToken);
            var total = OrderZoneFeeHelper.SumForVehicles(vehicles, request.DestinationZoneId, rates);

            return Result.Success(new CustomerOrderDeliveryFeesDto { DeliveryFees = total });
        }
    }
}
