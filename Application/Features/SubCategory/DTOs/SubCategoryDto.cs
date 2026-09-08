namespace Application.Features.SubCategory.DTOs
{
    public class SubCategoryDto
    {
        public int SubCategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public bool IsOffer { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public int VehicleCount { get; set; }
    }

    public class SubCategoryLookupDto
    {
        public int SubCategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}

