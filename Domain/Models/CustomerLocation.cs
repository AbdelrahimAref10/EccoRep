namespace Domain.Models
{
    public class CustomerLocation
    {
        public int CustomerLocationId { get; private set; }
        public int CustomerId { get; private set; }
        public double Longitude { get; private set; }
        public double Latitude { get; private set; }

        // Navigation property to Customer
        public Customer Customer { get; private set; } = null!;
        public DateTime LastModifiedDate { get; set; }

        private CustomerLocation() { }
        public static CustomerLocation Create(
            int customerId,
            double longitude,
            double latitude,
            DateTime lastModifiedDate)
        {
            // Validate coordinates
            if (longitude < -180 || longitude > 180)
                throw new ArgumentException("Longitude must be between -180 and 180", nameof(longitude));

            if (latitude < -90 || latitude > 90)
                throw new ArgumentException("Latitude must be between -90 and 90", nameof(latitude));

            if (customerId <= 0)
                throw new ArgumentException("Customer ID must be greater than zero", nameof(customerId));

            return new CustomerLocation
            {
                CustomerId = customerId,
                Longitude = longitude,
                Latitude = latitude,
                LastModifiedDate = lastModifiedDate
            };
        }

        public void UpdateLocation(double longitude, double latitude, DateTime lastModifiedDate)
        {
            // Validate coordinates
            if (longitude < -180 || longitude > 180)
                throw new ArgumentException("Longitude must be between -180 and 180", nameof(longitude));

            if (latitude < -90 || latitude > 90)
                throw new ArgumentException("Latitude must be between -90 and 90", nameof(latitude));

            Longitude = longitude;
            Latitude = latitude;
            LastModifiedDate = lastModifiedDate;
        }
    }
}

