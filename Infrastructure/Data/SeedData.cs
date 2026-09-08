using Domain.Enums;
using Domain.Models;
using Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data
{
    public static class SeedData
    {
        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("SeedData");
            var dbContext = services.GetRequiredService<DatabaseContext>();

            try
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    if (!await dbContext.Database.CanConnectAsync(cts.Token))
                    {
                        logger.LogWarning("Database is not available. Skipping seed data.");
                        return;
                    }
                }
                catch (Exception dbEx)
                {
                    logger.LogWarning(dbEx, "Cannot connect to database. Skipping seed data.");
                    return;
                }

                foreach (var roleName in AppRoleNames.All)
                {
                    bool roleExists;
                    try
                    {
                        roleExists = await roleManager.RoleExistsAsync(roleName);
                    }
                    catch (Microsoft.Data.SqlClient.SqlException sqlEx) when (sqlEx.Number == 208)
                    {
                        logger.LogWarning("Role table does not exist. Database migrations may not have been applied. Skipping seed data.");
                        return;
                    }

                    if (!roleExists)
                    {
                        var role = new ApplicationRole
                        {
                            Name = roleName,
                            NormalizedName = roleName.ToUpperInvariant(),
                            CreatedDate = DateTime.UtcNow,
                            LastModifiedDate = DateTime.UtcNow
                        };
                        await roleManager.CreateAsync(role);
                        logger.LogInformation("{RoleName} role created", roleName);
                    }
                }

                var adminUser = await userManager.FindByNameAsync("admin");
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = "admin",
                        NormalizedUserName = "ADMIN",
                        Email = "admin@ecommerce.com",
                        NormalizedEmail = "ADMIN@ECOMMERCE.COM",
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                        Active = true,
                        CreatedDate = DateTime.UtcNow,
                        LastModifiedDate = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(adminUser, "Admin@123");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, AppRoleNames.SuperAdmin);

                        dbContext.Employees.Add(Employee.Create(adminUser.Id, "System Admin", "System"));
                        await dbContext.SaveChangesAsync();

                        logger.LogInformation("Super Admin user created with username: admin, password: Admin@123");
                    }
                    else
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        logger.LogError("Failed to create admin user: {Errors}", errors);
                    }
                }
                else
                {
                    if (!await userManager.IsInRoleAsync(adminUser, AppRoleNames.SuperAdmin))
                    {
                        await userManager.AddToRoleAsync(adminUser, AppRoleNames.SuperAdmin);
                    }

                    if (!dbContext.Employees.Any(e => e.UserId == adminUser.Id))
                    {
                        dbContext.Employees.Add(Employee.Create(adminUser.Id, "System Admin", "System"));
                        await dbContext.SaveChangesAsync();
                    }

                    logger.LogInformation("Admin user already exists");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding admin user");
            }
        }
    }
}
