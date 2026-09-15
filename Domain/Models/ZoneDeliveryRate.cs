using Domain.Common;

namespace Domain.Models
{
    public class ZoneDeliveryRate : IAuditable
    {
        public int ZoneDeliveryRateId { get; private set; }
        public int ZoneGroupId { get; private set; }
        public int FromZoneId { get; private set; }
        public int ToZoneId { get; private set; }
        public decimal Fee { get; private set; }

        public ZoneGroup ZoneGroup { get; private set; } = null!;
        public Zone FromZone { get; private set; } = null!;
        public Zone ToZone { get; private set; } = null!;

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        private ZoneDeliveryRate() { }

        public static ZoneDeliveryRate Create(
            int zoneGroupId,
            int fromZoneId,
            int toZoneId,
            decimal fee,
            string? createdBy = null)
        {
            if (zoneGroupId <= 0)
                throw new ArgumentException("Zone group ID must be greater than zero", nameof(zoneGroupId));
            if (fromZoneId <= 0)
                throw new ArgumentException("From zone ID must be greater than zero", nameof(fromZoneId));
            if (toZoneId <= 0)
                throw new ArgumentException("To zone ID must be greater than zero", nameof(toZoneId));
            if (fee < 0)
                throw new ArgumentException("Fee cannot be negative", nameof(fee));

            return new ZoneDeliveryRate
            {
                ZoneGroupId = zoneGroupId,
                FromZoneId = fromZoneId,
                ToZoneId = toZoneId,
                Fee = fee,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        public void UpdateFee(decimal fee, string? modifiedBy = null)
        {
            if (fee < 0)
                throw new ArgumentException("Fee cannot be negative", nameof(fee));

            Fee = fee;
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}
