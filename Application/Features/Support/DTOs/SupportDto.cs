namespace Application.Features.Support.DTOs
{
    public class SupportDto
    {
        public int SupportId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? WhatsAppNumber { get; set; }
        public string? Email { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? TikTokUrl { get; set; }
        public string? YouTubeUrl { get; set; }
        public string? WorkingHours { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? AdditionalInfo { get; set; }
    }
}
