using Domain.Common;

namespace Domain.Models
{
    public class Zone : IAuditable
    {
        public int ZoneId { get; private set; }
        public int ZoneGroupId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public bool IsActive { get; private set; } = true;

        public ZoneGroup ZoneGroup { get; private set; } = null!;

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        private Zone() { }

        /// <summary>Great-circle distance in kilometres (no map provider).</summary>
        public static double DistanceKm(double fromLat, double fromLng, double toLat, double toLng)
        {
            const double earthRadiusKm = 6371.0;
            var dLat = ToRadians(toLat - fromLat);
            var dLng = ToRadians(toLng - fromLng);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(fromLat)) * Math.Cos(ToRadians(toLat))
                * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadiusKm * c;
        }

        public double DistanceKmTo(Zone other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return DistanceKm(Latitude, Longitude, other.Latitude, other.Longitude);
        }

        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
    }
}
