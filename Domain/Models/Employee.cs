using Domain.Common;

namespace Domain.Models
{
    public class Employee : IAuditable
    {
        public int EmployeeId { get; private set; }
        public int UserId { get; private set; }
        public string FullName { get; private set; } = string.Empty;
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }

        public ApplicationUser User { get; private set; } = null!;

        private Employee() { }

        public static Employee Create(int userId, string fullName, string? createdBy = null)
        {
            if (userId <= 0)
                throw new ArgumentException("User ID must be greater than zero", nameof(userId));

            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Full name cannot be empty", nameof(fullName));

            return new Employee
            {
                UserId = userId,
                FullName = fullName.Trim(),
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };
        }

        public void Update(string fullName, string? modifiedBy = null)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Full name cannot be empty", nameof(fullName));

            FullName = fullName.Trim();
            LastModifiedBy = modifiedBy;
            LastModifiedDate = DateTime.UtcNow;
        }
    }
}
