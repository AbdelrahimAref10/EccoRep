using System.Collections.Generic;
using System.Linq;

namespace Application.Features.Order.Common
{
    public static class ZoneProximitySorter
    {
        public static IReadOnlyList<T> SortByMerchantZoneDistance<T>(
            IEnumerable<T> items,
            Domain.Models.Zone origin,
            System.Func<T, int> merchantZoneId,
            IReadOnlyDictionary<int, Domain.Models.Zone> zonesById)
        {
            return items
                .OrderBy(item => merchantZoneId(item) == origin.ZoneId ? 0 : 1)
                .ThenBy(item =>
                {
                    if (!zonesById.TryGetValue(merchantZoneId(item), out var merchantZone))
                        return double.MaxValue;

                    return origin.DistanceKmTo(merchantZone);
                })
                .ThenBy(item => merchantZoneId(item))
                .ToList();
        }
    }
}
