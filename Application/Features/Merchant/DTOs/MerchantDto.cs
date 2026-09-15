namespace Application.Features.Merchant.DTOs
{
    public class MerchantDto
    {
        public int MerchantId { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public int CityId { get; set; }
        public int ZoneId { get; set; }
        public string? CityName { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PersonalImage { get; set; }
        public bool IsActive { get; set; }
        public bool CashOnReceive { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
