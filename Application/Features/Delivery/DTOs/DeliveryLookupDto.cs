namespace Application.Features.Delivery.DTOs
{
    /// <summary>
    /// Lightweight delivery row for shared lookup (admin assign delivery, etc.).
    /// </summary>
    public class DeliveryLookupDto
    {
        public int DeliveryId { get; set; }
        public int CityId { get; set; }
        public int ZoneId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
    }
}
