using Domain.Common;

namespace Domain.Models
{
    public class CompanyTreasury
    {
        // Private setters for encapsulation
        public int Id { get; private set; }
        public decimal DebitAmount { get; private set; } = 0;
        public decimal CreditAmount { get; private set; } = 0;
        public string DescriptionAr { get; private set; } = string.Empty;
        public string DescriptionEng { get; private set; } = string.Empty;

        // Audit properties
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }

        // Private constructor for EF Core
        private CompanyTreasury() { }

        // Factory method for creating treasury record
        public static CompanyTreasury Create(
            decimal debitAmount,
            decimal creditAmount,
            string descriptionAr,
            string descriptionEng,
            string? createdBy = null)
        {
            if (debitAmount < 0)
                throw new ArgumentException("Debit amount cannot be negative", nameof(debitAmount));
            
            if (creditAmount < 0)
                throw new ArgumentException("Credit amount cannot be negative", nameof(creditAmount));

            return new CompanyTreasury
            {
                DebitAmount = debitAmount,
                CreditAmount = creditAmount,
                DescriptionAr = descriptionAr ?? string.Empty,
                DescriptionEng = descriptionEng ?? string.Empty,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow
            };
        }
    }
}

