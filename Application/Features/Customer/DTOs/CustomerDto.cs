using Domain.Enums;

namespace Application.Features.Customer.DTOs
{
    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string MobileNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string? PersonalImage { get; set; }
        public string? Email { get; set; }
        public string? CommercialRegisterImage { get; set; }
        public int RegisterAs { get; set; }
        public int VerificationBy { get; set; }
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public CustomerState State { get; set; }
        public bool CashBlock { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

