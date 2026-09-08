using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Vehicle.Query.GetVehicleStatisticsQuery
{
    public record GetVehicleStatisticsQuery : IRequest<Result<VehicleStatisticsDto>>
    {
        public int? CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
    }

    public class GetVehicleStatisticsQueryHandler : IRequestHandler<GetVehicleStatisticsQuery, Result<VehicleStatisticsDto>>
    {
        private readonly DatabaseContext _context;

        public GetVehicleStatisticsQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<VehicleStatisticsDto>> Handle(GetVehicleStatisticsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Vehicles.AsNoTracking().AsQueryable();

            if (request.CategoryId.HasValue)
            {
                query = query.Where(v => v.SubCategory.CategoryId == request.CategoryId.Value);
            }

            if (request.SubCategoryId.HasValue)
            {
                query = query.Where(v => v.SubCategoryId == request.SubCategoryId.Value);
            }

            var totalVehicles = await query.CountAsync(cancellationToken);
            var availableNow = await query.CountAsync(v => v.Status == VehicleStatus.Available, cancellationToken);
            var underMaintenance = await query.CountAsync(v => v.Status == VehicleStatus.UnderMaintenance, cancellationToken);

            var statistics = new VehicleStatisticsDto
            {
                TotalVehicles = totalVehicles,
                AvailableNow = availableNow,
                UnderMaintenance = underMaintenance
            };

            return Result.Success(statistics);
        }
    }

    public class VehicleStatisticsDto
    {
        public int TotalVehicles { get; set; }
        public int AvailableNow { get; set; }
        public int UnderMaintenance { get; set; }
    }
}
