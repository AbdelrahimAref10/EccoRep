using Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Domain.Models
{
    public class ApplicationRole : IdentityRole<int>, IAuditable
    {
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }
    }
}

