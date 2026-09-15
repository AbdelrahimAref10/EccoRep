using Domain.Models;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Order.Common
{
    public static class OrderZoneFeeHelper
    {
        public static async Task<List<ZoneDeliveryRate>> LoadRatesForCityAsync(
            DatabaseContext context,
            int cityId,
            CancellationToken cancellationToken)
        {
            var city = await context.Cities
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CityId == cityId, cancellationToken);

            if (city?.ZoneGroupId == null)
                return new List<ZoneDeliveryRate>();

            return await context.ZoneDeliveryRates
                .AsNoTracking()
                .Where(r => r.ZoneGroupId == city.ZoneGroupId.Value)
                .ToListAsync(cancellationToken);
        }

        public static async Task<bool> ZoneBelongsToCityAsync(
            DatabaseContext context,
            int cityId,
            int zoneId,
            CancellationToken cancellationToken)
        {
            var city = await context.Cities
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CityId == cityId, cancellationToken);

            if (city?.ZoneGroupId == null)
                return false;

            return await context.Zones.AsNoTracking().AnyAsync(
                z => z.ZoneId == zoneId && z.ZoneGroupId == city.ZoneGroupId.Value && z.IsActive,
                cancellationToken);
        }

        public static decimal SumForVehicles(
            IReadOnlyCollection<Domain.Models.Vehicle> vehicles,
            int destinationZoneId,
            IReadOnlyCollection<ZoneDeliveryRate> rates)
        {
            var zoneIds = vehicles.Select(v => v.Merchant.ZoneId).ToList();
            return Domain.Models.Order.SumVehicleDeliveryFees(zoneIds, destinationZoneId, rates);
        }

        public static IReadOnlyList<(int VehicleId, decimal Fee)> FeesByVehicle(
            IReadOnlyCollection<Domain.Models.Vehicle> vehicles,
            int destinationZoneId,
            IReadOnlyCollection<ZoneDeliveryRate> rates)
        {
            return vehicles
                .Select(v => (
                    v.VehicleId,
                    Domain.Models.Order.CalculateVehicleDeliveryFee(v.Merchant.ZoneId, destinationZoneId, rates)))
                .ToList();
        }
    }
}
