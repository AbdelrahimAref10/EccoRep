using Application.Features.Delivery.DTOs;
using CSharpFunctionalExtensions;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Delivery.Query.GetActiveDeliveriesLookupQuery
{
    public record GetActiveDeliveriesLookupQuery : IRequest<Result<List<DeliveryLookupDto>>>
    {
        /// <summary>When set, returns only active deliveries in this city (e.g. order city).</summary>
        public int? CityId { get; set; }
    }

    public class GetActiveDeliveriesLookupQueryHandler
        : IRequestHandler<GetActiveDeliveriesLookupQuery, Result<List<DeliveryLookupDto>>>
    {
        private readonly DatabaseContext _context;

        public GetActiveDeliveriesLookupQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<List<DeliveryLookupDto>>> Handle(
            GetActiveDeliveriesLookupQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Deliveries
                .AsNoTracking()
                .Where(d => d.IsActive && !d.IsDeleted);

            if (request.CityId is > 0)
                query = query.Where(d => d.CityId == request.CityId.Value);

            var deliveries = await query
                .OrderBy(d => d.FullName)
                .Select(d => new DeliveryLookupDto
                {
                    DeliveryId = d.DeliveryId,
                    CityId = d.CityId,
                    FullName = d.FullName,
                    MobileNumber = d.MobileNumber
                })
                .ToListAsync(cancellationToken);

            return Result.Success(deliveries);
        }
    }
}
