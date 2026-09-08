namespace Application.Features.User.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool LockoutEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
        public bool Active { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public bool IsActive { get; set; }
    }
}


