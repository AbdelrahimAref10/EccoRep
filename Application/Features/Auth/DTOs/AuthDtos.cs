namespace Application.Features.Auth.DTOs
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
        /// <summary>AppRole enum int used for this login: Customer=1, SuperAdmin=2, Merchant=3, Delivery=4.</summary>
        public int Role { get; set; }
        public int? CustomerId { get; set; }
        public int? MerchantId { get; set; }
        public int? DeliveryId { get; set; }
        public int? EmployeeId { get; set; }
        public string? PersonalImage { get; set; }
        public int? CityId { get; set; }
        public string? CityName { get; set; }
    }

    public class RegisterResponse
    {
        public int UserId { get; set; }
        public int? CustomerId { get; set; }
        public int? MerchantId { get; set; }
        public int? DeliveryId { get; set; }
        public string InvitationCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class RefreshTokenResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class MessageResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
