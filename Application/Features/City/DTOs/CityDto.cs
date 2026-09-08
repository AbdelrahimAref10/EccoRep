using System;
using System.Collections.Generic;

namespace Application.Features.City.DTOs
{
    public class CityDto
    {
        public int CityId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int CustomerCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal? DeliveryFees { get; set; }
        public decimal? UrgentDelivery { get; set; }
        public decimal? ServiceFees { get; set; }
        public decimal? CancellationFees { get; set; }
        public List<TieredDiscountDto> TieredDiscounts { get; set; } = new List<TieredDiscountDto>();
    }
}

