using Application.Features.City.DTOs;

namespace Application.Features.Order.DTOs
{
    public class CityFeesDto
    {
        public int CityId { get; set; }
        public decimal? ServiceFees { get; set; } // Percentage value
        public decimal? UrgentFees { get; set; }
        public decimal? CancellationFees { get; set; }
        /// <summary>
        /// Sum of pending OrderCancellationFees Withdraw amounts for the logged-in customer.
        /// </summary>
        public decimal PreviousDebt { get; set; }
        public List<TieredDiscountDto> TieredDiscounts { get; set; } = new();
    }
}
