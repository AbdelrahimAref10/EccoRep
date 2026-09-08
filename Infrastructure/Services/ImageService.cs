using System;
using System.IO;
using System.Text;

namespace Infrastructure.Services
{
    public interface IImageService
    {
        string SaveBase64Image(string base64String, string folderName = "uploads");
        string? GetImageUrl(string fileName);
        bool DeleteImage(string fileName);
        bool IsBase64String(string value);
    }

    public class ImageService : IImageService
    {
        private readonly string _uploadsPath;
        private readonly string _baseUrl;

        public ImageService()
        {
            // For development, save to wwwroot/uploads
            _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            _baseUrl = "/uploads";

            // Ensure directory exists
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
            }
        }

        public string SaveBase64Image(string base64String, string folderName = "uploads")
        {
            if (string.IsNullOrWhiteSpace(base64String))
            {
                throw new ArgumentException("Base64 string cannot be empty", nameof(base64String));
            }

            // Remove data URL prefix if present (e.g., "data:image/png;base64,")
            var base64Data = base64String.Contains(",") 
                ? base64String.Split(',')[1] 
                : base64String;

            try
            {
                var imageBytes = Convert.FromBase64String(base64Data);
                
                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}.jpg";
                var folderPath = Path.Combine(_uploadsPath, folderName);
                
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var filePath = Path.Combine(folderPath, fileName);
                File.WriteAllBytes(filePath, imageBytes);

                // Return relative URL
                return $"{_baseUrl}/{folderName}/{fileName}";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error saving image: {ex.Message}", ex);
            }
        }

        public string? GetImageUrl(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            // If already a full URL, return as is
            if (fileName.StartsWith("http://") || fileName.StartsWith("https://") || fileName.StartsWith("/"))
            {
                return fileName;
            }

            // Check if it's a base64 string (long string without slashes, starts with data: or is very long)
            // Base64 strings are typically long and don't contain path separators
            if (fileName.StartsWith("data:image/") || 
                (fileName.Length > 100 && !fileName.Contains("/") && !fileName.Contains("\\")))
            {
                // This is likely base64 data - return null as we can't serve it as a URL
                // In production, you might want to log this and migrate the data
                return null;
            }

            return $"{_baseUrl}/{fileName}";
        }

        public bool DeleteImage(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            try
            {
                // Extract filename from URL if needed
                var actualFileName = fileName.Contains("/") 
                    ? Path.GetFileName(fileName) 
                    : fileName;

                var filePath = Path.Combine(_uploadsPath, actualFileName);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool IsBase64String(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            // Check if it starts with data:image/ (base64 data URL)
            if (value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check if it's a URL (starts with http://, https://, or /)
            if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Try to detect if it's base64 by attempting to decode it
            // Base64 strings are typically long and don't contain path separators
            if (value.Length > 100 && !value.Contains("/") && !value.Contains("\\"))
            {
                try
                {
                    // Remove data URL prefix if present
                    var base64Data = value.Contains(",") 
                        ? value.Split(',')[1] 
                        : value;
                    
                    // Try to decode - if it succeeds, it's likely base64
                    Convert.FromBase64String(base64Data);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }
}


