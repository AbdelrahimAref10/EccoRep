using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Configuration
{
    public static class FireBaseConfigurations
    {
        public static IServiceCollection AddFireBaseConfigurations(
            this IServiceCollection services, 
            IConfiguration configuration, 
            string contentRootPath)
        {
            try
            {
                var path = Path.Combine(contentRootPath, "FireBaseConfigurations.json");
                
                if (!File.Exists(path))
                {
                    // Log warning but don't fail - allows app to start without Firebase config
                    Console.WriteLine($"Warning: FireBaseConfigurations.json not found at {path}. Push notifications will not work.");
                    return services;
                }

                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(path)
                });

                Console.WriteLine("Firebase initialized successfully");
            }
            catch (Exception ex)
            {
                // Log error but don't fail - allows app to start without Firebase config
                Console.WriteLine($"Warning: Failed to initialize Firebase: {ex.Message}. Push notifications will not work.");
            }

            return services;
        }
    }
}

