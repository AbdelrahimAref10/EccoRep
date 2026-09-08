using Domain.Enums;

namespace Application.Features.Customer.DTOs
{
    /// <summary>
    /// Lightweight customer row for admin phone lookup (order create, etc.).
    /// </summary>
    public class CustomerLookupDto
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public CustomerState State { get; set; }
        public bool CashBlock { get; set; }
    }
}
