using Domain.Common;

namespace Domain.Models
{
    public class ZoneGroup : IAuditable
    {
        public int ZoneGroupId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public bool IsActive { get; private set; } = true;

        public ICollection<Zone> Zones { get; private set; } = new List<Zone>();
        public ICollection<ZoneDeliveryRate> DeliveryRates { get; private set; } = new List<ZoneDeliveryRate>();
        public ICollection<City> Cities { get; private set; } = new List<City>();

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        private ZoneGroup() { }
    }
}
