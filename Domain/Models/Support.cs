using Domain.Common;

namespace Domain.Models
{
    /// <summary>
    /// Company support / contact information (typically a single row).
    /// </summary>
    public class Support : IAuditable
    {
        public int SupportId { get; private set; }
        public string CompanyName { get; private set; } = string.Empty;
        public string? Address { get; private set; }
        public string? PhoneNumber { get; private set; }
        public string? WhatsAppNumber { get; private set; }
        public string? Email { get; private set; }
        public string? WebsiteUrl { get; private set; }
        public string? FacebookUrl { get; private set; }
        public string? InstagramUrl { get; private set; }
        public string? TwitterUrl { get; private set; }
        public string? LinkedInUrl { get; private set; }
        public string? TikTokUrl { get; private set; }
        public string? YouTubeUrl { get; private set; }
        public string? WorkingHours { get; private set; }
        public decimal? Latitude { get; private set; }
        public decimal? Longitude { get; private set; }
        public string? AdditionalInfo { get; private set; }

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        private Support() { }

        public static Support Create(
            string companyName,
            string? address = null,
            string? phoneNumber = null,
            string? whatsAppNumber = null,
            string? email = null,
            string? websiteUrl = null,
            string? facebookUrl = null,
            string? instagramUrl = null,
            string? twitterUrl = null,
            string? linkedInUrl = null,
            string? tikTokUrl = null,
            string? youTubeUrl = null,
            string? workingHours = null,
            decimal? latitude = null,
            decimal? longitude = null,
            string? additionalInfo = null,
            string? createdBy = null)
        {
            if (string.IsNullOrWhiteSpace(companyName))
                throw new ArgumentException("Company name is required", nameof(companyName));

            return new Support
            {
                CompanyName = companyName.Trim(),
                Address = Normalize(address),
                PhoneNumber = Normalize(phoneNumber),
                WhatsAppNumber = Normalize(whatsAppNumber),
                Email = Normalize(email),
                WebsiteUrl = Normalize(websiteUrl),
                FacebookUrl = Normalize(facebookUrl),
                InstagramUrl = Normalize(instagramUrl),
                TwitterUrl = Normalize(twitterUrl),
                LinkedInUrl = Normalize(linkedInUrl),
                TikTokUrl = Normalize(tikTokUrl),
                YouTubeUrl = Normalize(youTubeUrl),
                WorkingHours = Normalize(workingHours),
                Latitude = latitude,
                Longitude = longitude,
                AdditionalInfo = Normalize(additionalInfo),
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        public void Update(
            string companyName,
            string? address = null,
            string? phoneNumber = null,
            string? whatsAppNumber = null,
            string? email = null,
            string? websiteUrl = null,
            string? facebookUrl = null,
            string? instagramUrl = null,
            string? twitterUrl = null,
            string? linkedInUrl = null,
            string? tikTokUrl = null,
            string? youTubeUrl = null,
            string? workingHours = null,
            decimal? latitude = null,
            decimal? longitude = null,
            string? additionalInfo = null,
            string? modifiedBy = null)
        {
            if (string.IsNullOrWhiteSpace(companyName))
                throw new ArgumentException("Company name is required", nameof(companyName));

            CompanyName = companyName.Trim();
            Address = Normalize(address);
            PhoneNumber = Normalize(phoneNumber);
            WhatsAppNumber = Normalize(whatsAppNumber);
            Email = Normalize(email);
            WebsiteUrl = Normalize(websiteUrl);
            FacebookUrl = Normalize(facebookUrl);
            InstagramUrl = Normalize(instagramUrl);
            TwitterUrl = Normalize(twitterUrl);
            LinkedInUrl = Normalize(linkedInUrl);
            TikTokUrl = Normalize(tikTokUrl);
            YouTubeUrl = Normalize(youTubeUrl);
            WorkingHours = Normalize(workingHours);
            Latitude = latitude;
            Longitude = longitude;
            AdditionalInfo = Normalize(additionalInfo);
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }

        private static string? Normalize(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
