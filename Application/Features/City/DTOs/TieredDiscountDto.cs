namespace Application.Features.City.DTOs
{
    public class TieredDiscountDto
    {
        public int Id { get; set; }
        public int CityId { get; set; }
        public decimal From { get; set; }
        public decimal To { get; set; }
        public decimal Discount { get; set; }
    }
}

