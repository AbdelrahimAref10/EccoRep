namespace Application.Features.Customer.DTOs
{
    /// <summary>
    /// Request DTO for saving customer location
    /// </summary>
    public class CustomerLocationRequestDto
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }
}

