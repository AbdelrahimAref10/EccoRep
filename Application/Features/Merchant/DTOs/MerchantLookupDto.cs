namespace Application.Features.Merchant.DTOs
{
    /// <summary>
    /// Lightweight merchant row for shared lookup (admin create order / vehicle, mobile, etc.).
    /// </summary>
    public class MerchantLookupDto
    {
        public int MerchantId { get; set; }
        public int CityId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public bool CashOnReceive { get; set; }
    }
}
