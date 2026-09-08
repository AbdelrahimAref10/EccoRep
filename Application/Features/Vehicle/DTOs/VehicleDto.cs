namespace Application.Features.Vehicle.DTOs
{
    public class VehicleDto
    {
        public int VehicleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string VehicleCode { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        /// <summary>VehicleStatus as int: Available=0, UnderMaintenance=1, Rented=2.</summary>
        public int Status { get; set; }
        public int SubCategoryId { get; set; }
        public string SubCategoryName { get; set; } = string.Empty;
        public decimal SubCategoryPrice { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
    }
}
