using CSharpFunctionalExtensions;
using Domain.Enums;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Services
{
    public sealed class VehicleReservationQueryService : IVehicleReservationQueryService
    {
        private readonly DatabaseContext _context;

        public VehicleReservationQueryService(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Result<IReadOnlyList<VehicleReservationAvailabilityItem>>> GetAvailableVehiclesAsync(
            int subCategoryId,
            int cityId,
            DateTime reservationDateFrom,
            DateTime reservationDateTo,
            CancellationToken cancellationToken = default,
            int? excludeOrderId = null)
        {
            var dateRange = ValidateDateRange(reservationDateFrom, reservationDateTo, requireNotInPast: true);
            if (dateRange.IsFailure)
            {
                return Result.Failure<IReadOnlyList<VehicleReservationAvailabilityItem>>(dateRange.Error);
            }

            var (from, to) = dateRange.Value;

            var subCategory = await _context.SubCategories
                .AsNoTracking()
                .Include(sc => sc.Category)
                .FirstOrDefaultAsync(
                    sc => sc.SubCategoryId == subCategoryId && sc.IsActive,
                    cancellationToken);

            if (subCategory == null)
            {
                return Result.Failure<IReadOnlyList<VehicleReservationAvailabilityItem>>(
                    "SubCategory not found or inactive");
            }

            if (subCategory.Category.CityId != cityId)
            {
                return Result.Failure<IReadOnlyList<VehicleReservationAvailabilityItem>>(
                    "SubCategory does not belong to the selected city");
            }

            var vehicles = await _context.Vehicles
                .AsNoTracking()
                .Where(v => v.SubCategoryId == subCategoryId
                    && v.SubCategory.Category.CityId == cityId
                    && v.Status != VehicleStatus.UnderMaintenance
                    && v.MerchantId > 0
                    && !v.Merchant.IsDeleted)
                .OrderBy(v => v.Name)
                .Select(v => new
                {
                    v.VehicleId,
                    v.Name,
                    v.VehicleCode,
                    v.ImageUrl,
                    v.Status,
                    v.MerchantId,
                    MerchantZoneId = v.Merchant.ZoneId,
                    MerchantName = v.Merchant.FullName,
                    v.Color,
                    v.Type,
                    v.Model,
                    v.Price,
                    v.SpeedKmh,
                    v.EngineCapacityCc
                })
                .ToListAsync(cancellationToken);

            var vehicleIds = vehicles.Select(v => v.VehicleId).ToList();
            var reservationsByVehicle = await GetActiveReservationsByVehicleAsync(
                vehicleIds, from, to, excludeOrderId, cancellationToken);

            IReadOnlyList<VehicleReservationAvailabilityItem> items = vehicles
                .Select(v =>
                {
                    var conflicting = reservationsByVehicle.TryGetValue(v.VehicleId, out var ranges)
                        ? ExpandDatesInRange(ranges, from, to)
                        : (IReadOnlyList<DateTime>)Array.Empty<DateTime>();

                    return new VehicleReservationAvailabilityItem
                    {
                        VehicleId = v.VehicleId,
                        Name = v.Name,
                        VehicleCode = v.VehicleCode,
                        ImagePath = v.ImageUrl,
                        VehicleStatus = v.Status,
                        MerchantId = v.MerchantId,
                        MerchantZoneId = v.MerchantZoneId,
                        MerchantName = v.MerchantName,
                        Color = v.Color,
                        Type = v.Type,
                        Model = v.Model,
                        Price = v.Price,
                        SpeedKmh = v.SpeedKmh,
                        EngineCapacityCc = v.EngineCapacityCc,
                        AvailabilityStatus = conflicting.Count > 0
                            ? VehicleAvailabilityStatus.Reserved
                            : VehicleAvailabilityStatus.Available,
                        ConflictingDates = conflicting
                    };
                })
                .ToList();

            return Result.Success(items);
        }

        private async Task<Dictionary<int, List<(DateTime DateFrom, DateTime DateTo)>>> GetActiveReservationsByVehicleAsync(
            IReadOnlyCollection<int> vehicleIds,
            DateTime from,
            DateTime to,
            int? excludeOrderId,
            CancellationToken cancellationToken)
        {
            if (vehicleIds.Count == 0)
            {
                return new Dictionary<int, List<(DateTime, DateTime)>>();
            }

            var query = _context.ReservedVehiclesPerDays
                .AsNoTracking()
                .Where(rv => vehicleIds.Contains(rv.VehicleId)
                    && rv.State == ReservedVehicleState.StillBooked
                    && rv.Order.OrderState != OrderState.Completed
                    && rv.Order.OrderState != OrderState.Cancelled
                    && rv.DateFrom <= to
                    && rv.DateTo >= from);

            if (excludeOrderId.HasValue)
            {
                query = query.Where(rv => rv.OrderId != excludeOrderId.Value);
            }

            var rows = await query
                .Select(rv => new { rv.VehicleId, rv.DateFrom, rv.DateTo })
                .ToListAsync(cancellationToken);

            return rows
                .GroupBy(r => r.VehicleId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => (x.DateFrom, x.DateTo)).ToList());
        }

        private static Result<(DateTime From, DateTime To)> ValidateDateRange(
            DateTime reservationDateFrom,
            DateTime reservationDateTo,
            bool requireNotInPast)
        {
            var from = reservationDateFrom.Date;
            var to = reservationDateTo.Date;

            if (from > to)
            {
                return Result.Failure<(DateTime, DateTime)>(
                    "Reservation date from must be on or before reservation date to");
            }

            if (requireNotInPast && from < DateTime.UtcNow.Date && from < DateTime.Now.Date)
            {
                return Result.Failure<(DateTime, DateTime)>(
                    "Reservation date from cannot be in the past");
            }

            return Result.Success((from, to));
        }

        private static IReadOnlyList<DateTime> ExpandDatesInRange(
            IEnumerable<(DateTime DateFrom, DateTime DateTo)> ranges,
            DateTime rangeFrom,
            DateTime rangeTo)
        {
            var dates = new HashSet<DateTime>();

            foreach (var (dateFrom, dateTo) in ranges)
            {
                var current = dateFrom.Date;
                if (current < rangeFrom)
                {
                    current = rangeFrom;
                }

                var end = dateTo.Date;
                if (end > rangeTo)
                {
                    end = rangeTo;
                }

                while (current <= end)
                {
                    dates.Add(current);
                    current = current.AddDays(1);
                }
            }

            return dates.OrderBy(d => d).ToList();
        }
    }
}
