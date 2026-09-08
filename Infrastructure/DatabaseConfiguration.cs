using Domain.Models;
using Infrastructure.Configuration;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DatabaseConfiguration
    {
        public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register strongly-typed configuration settings
            services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

            // Register settings as singletons for direct injection
            services.AddSingleton<IJwtSettings>(sp =>
            {
                var jwtSettings = new JwtSettings();
                configuration.GetSection("Jwt").Bind(jwtSettings);
                return jwtSettings;
            });

            services.AddDbContext<DatabaseContext>(options =>
                options.UseSqlServer(
                        configuration.GetConnectionString("DefaultConnection")
                )
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            );

            // Configure Identity with int primary keys
            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;

                // User settings
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<DatabaseContext>()
            .AddDefaultTokenProviders();

            // Configure JWT Authentication
            services.AddJwtAuthentication(configuration);

            // Register JWT Token Service
            services.AddScoped<Infrastructure.Services.IJwtTokenService, Infrastructure.Services.JwtTokenService>();

            // Register Email Service
            services.AddScoped<Infrastructure.Services.IEmailService, Infrastructure.Services.EmailService>();

            // Register SMS Service
            services.AddScoped<Infrastructure.Services.ISmsService, Infrastructure.Services.SmsService>();

            // Register Invitation Code Service
            services.AddScoped<Infrastructure.Services.IInvitationCodeService, Infrastructure.Services.InvitationCodeService>();

            // Register User Session
            services.AddHttpContextAccessor();
            services.AddScoped<Domain.Common.IUserSession, Infrastructure.Services.UserSession>();

            // Register Image Service
            services.AddScoped<Infrastructure.Services.IImageService, Infrastructure.Services.ImageService>();

            // Register HttpClientFactory for PayPal service
            services.AddHttpClient();

            // Register PayPal Service
            services.AddScoped<Infrastructure.Services.IPayPalService, Infrastructure.Services.PayPalService>();

            // Register Notification Service
            services.AddScoped<Infrastructure.Services.INotificationService, Infrastructure.Services.NotificationService>();

            // Register DateTime Provider as Singleton (stateless service, can be used in middleware)
            services.AddSingleton<Domain.Common.IDateTimeProvider, Infrastructure.Services.DateTimeProvider>();

            return services;
        }
    }
}
