namespace Application.Features.Category.DTOs
{
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int SubCategoryCount { get; set; }
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
    }

    public class CategoryLookupDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}


