using CSharpFunctionalExtensions;
using Domain.Common;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Zone.Query.GetZoneDeliveryMatrixQuery
{
    public record ZoneDeliveryMatrixRowDto
    {
        public int ToZoneId { get; set; }
        public string ToZoneName { get; set; } = string.Empty;
        public decimal Fee { get; set; }
    }

    public record ZoneDeliveryMatrixDto
    {
        public int FromZoneId { get; set; }
        public string FromZoneName { get; set; } = string.Empty;
        public int ZoneGroupId { get; set; }
        public List<ZoneDeliveryMatrixRowDto> Rates { get; set; } = new();
    }

    public record GetZoneDeliveryMatrixQuery : IRequest<Result<ZoneDeliveryMatrixDto>>
    {
        public int FromZoneId { get; set; }
    }

    public class GetZoneDeliveryMatrixQueryHandler : IRequestHandler<GetZoneDeliveryMatrixQuery, Result<ZoneDeliveryMatrixDto>>
    {
        private readonly DatabaseContext _context;

        public GetZoneDeliveryMatrixQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<ZoneDeliveryMatrixDto>> Handle(GetZoneDeliveryMatrixQuery request, CancellationToken cancellationToken)
        {
            if (request.FromZoneId <= 0)
                return Result.Failure<ZoneDeliveryMatrixDto>("From zone is required");

            var fromZone = await _context.Zones.AsNoTracking()
                .FirstOrDefaultAsync(z => z.ZoneId == request.FromZoneId, cancellationToken);

            if (fromZone == null)
                return Result.Failure<ZoneDeliveryMatrixDto>("Zone not found");

            var zones = await _context.Zones.AsNoTracking()
                .Where(z => z.ZoneGroupId == fromZone.ZoneGroupId && z.IsActive)
                .OrderBy(z => z.Name)
                .ToListAsync(cancellationToken);

            var rates = await _context.ZoneDeliveryRates.AsNoTracking()
                .Where(r => r.FromZoneId == request.FromZoneId)
                .ToListAsync(cancellationToken);

            var dto = new ZoneDeliveryMatrixDto
            {
                FromZoneId = fromZone.ZoneId,
                FromZoneName = fromZone.Name,
                ZoneGroupId = fromZone.ZoneGroupId,
                Rates = zones.Select(z =>
                {
                    var match = rates.FirstOrDefault(r => r.ToZoneId == z.ZoneId);
                    return new ZoneDeliveryMatrixRowDto
                    {
                        ToZoneId = z.ZoneId,
                        ToZoneName = z.Name,
                        Fee = match?.Fee ?? 0
                    };
                }).ToList()
            };

            return Result.Success(dto);
        }
    }
}
