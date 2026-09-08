using Application.Features.Order.DTOs;
using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Query.GetReservedVehiclePerSubCategoryQuery
{
    public record GetReservedVehiclePerSubCategoryQuery : IRequest<Result<List<ReservedDateDto>>>
    {
        public int SubCategoryId { get; set; }
    }

    public class GetReservedVehiclePerSubCategoryQueryHandler : IRequestHandler<GetReservedVehiclePerSubCategoryQuery, Result<List<ReservedDateDto>>>
    {
        private readonly DatabaseContext _context;

        public GetReservedVehiclePerSubCategoryQueryHandler(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<List<ReservedDateDto>>> Handle(GetReservedVehiclePerSubCategoryQuery request, CancellationToken cancellationToken)
        {
            // Total vehicles in this subcategory
            var totalVehicles = await _context.Vehicles
                .CountAsync(v => v.SubCategoryId == request.SubCategoryId, cancellationToken);

            if (totalVehicles == 0)
            {
                return Result.Failure<List<ReservedDateDto>>("No vehicles found in this subcategory");
            }

            // Get all reserved records from ReservedVehiclesPerDays for this subcategory (StillBooked, order still active)
            var reservedRecords = await _context.ReservedVehiclesPerDays
                .Include(rv => rv.Order)
                .Where(rv => rv.SubCategoryId == request.SubCategoryId
                    && rv.State == ReservedVehicleState.StillBooked
                    && rv.Order.OrderState != OrderState.Completed && rv.Order.OrderState != OrderState.Cancelled)
                .ToListAsync(cancellationToken);

            // Build reserved count per date (only dates that appear in the table)
            var reservedCountByDate = new Dictionary<DateTime, int>();

            foreach (var record in reservedRecords)
            {
                var currentDate = record.DateFrom.Date;
                var endDate = record.DateTo.Date;

                while (currentDate <= endDate)
                {
                    if (!reservedCountByDate.TryGetValue(currentDate, out var count))
                        count = 0;
                    reservedCountByDate[currentDate] = count + 1;
                    currentDate = currentDate.AddDays(1);
                }
            }

            // Return only dates that appear in ReservedVehiclesPerDays where ALL vehicles in subcategory are booked
            var fullyBookedDates = reservedCountByDate
                .Where(kv => kv.Value >= totalVehicles)
                .Select(kv => new ReservedDateDto { Date = kv.Key })
                .OrderBy(d => d.Date)
                .ToList();

            return Result.Success(fullyBookedDates);
        }
    }
}

