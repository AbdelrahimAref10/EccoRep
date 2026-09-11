using CSharpFunctionalExtensions;
using Domain.Enums;
using Domain.Models;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Order.Common
{
    public static class OrderCapacityHelper
    {
        public static async Task<Result> ValidateSubCategoryCapacityAsync(
            DatabaseContext context,
            int subCategoryId,
            DateTime reservationDateFrom,
            DateTime reservationDateTo,
            int vehiclesCount,
            int? excludeOrderId,
            CancellationToken cancellationToken)
        {
            var from = reservationDateFrom.Date;
            var to = reservationDateTo.Date;

            var bookableVehiclesCount = await context.Vehicles
                .CountAsync(v => v.SubCategoryId == subCategoryId
                    && v.Status != VehicleStatus.UnderMaintenance, cancellationToken);

            var reservedQuery = context.ReservedVehiclesPerDays
                .AsNoTracking()
                .Include(rv => rv.Order)
                .Where(rv => rv.SubCategoryId == subCategoryId
                    && rv.State == ReservedVehicleState.StillBooked
                    && rv.Order.OrderState != OrderState.Completed
                    && rv.Order.OrderState != OrderState.Cancelled
                    && rv.DateFrom <= to
                    && rv.DateTo >= from);

            if (excludeOrderId.HasValue)
            {
                reservedQuery = reservedQuery.Where(rv => rv.OrderId != excludeOrderId.Value);
            }

            var stillBookedOverlaps = await reservedQuery
                .Select(rv => new ValueTuple<DateTime, DateTime>(rv.DateFrom, rv.DateTo))
                .ToListAsync(cancellationToken);

            return Domain.Models.Order.EnsureSubCategoryHasCapacity(
                vehiclesCount,
                bookableVehiclesCount,
                from,
                to,
                stillBookedOverlaps);
        }
    }
}
