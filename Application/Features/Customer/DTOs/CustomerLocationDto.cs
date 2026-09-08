namespace Application.Features.Customer.DTOs
{
    /// <summary>
    /// Data Transfer Object for Customer Location
    /// </summary>
    public class CustomerLocationDto
    {
        public int CustomerId { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }
}

