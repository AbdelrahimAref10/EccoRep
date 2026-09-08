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
        private const string AdminUserName = "admin";
        private const string AdminPassword = "Admin@123";

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

                var adminUser = await userManager.FindByNameAsync(AdminUserName);
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = AdminUserName,
                        NormalizedUserName = "ADMIN",
                        Email = "admin@ecommerce.com",
                        NormalizedEmail = "ADMIN@ECOMMERCE.COM",
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                        Active = true,
                        CreatedDate = DateTime.UtcNow,
                        LastModifiedDate = DateTime.UtcNow
                    };

                    var result = await userManager.CreateAsync(adminUser, AdminPassword);
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        logger.LogError("Failed to create admin user: {Errors}", errors);
                        return;
                    }

                    logger.LogInformation("Super Admin user created with username: {UserName}, password: {Password}", AdminUserName, AdminPassword);
                }
                else
                {
                    // Keep seed admin usable after identity/role migrations.
                    var needsUpdate = false;
                    if (!adminUser.Active)
                    {
                        adminUser.Active = true;
                        needsUpdate = true;
                    }
                    if (!adminUser.EmailConfirmed)
                    {
                        adminUser.EmailConfirmed = true;
                        needsUpdate = true;
                    }
                    if (needsUpdate)
                        await userManager.UpdateAsync(adminUser);

                    // If seed password no longer works (e.g. after migrations), restore it.
                    if (!await userManager.CheckPasswordAsync(adminUser, AdminPassword))
                    {
                        if (await userManager.HasPasswordAsync(adminUser))
                            await userManager.RemovePasswordAsync(adminUser);

                        var passwordResult = await userManager.AddPasswordAsync(adminUser, AdminPassword);
                        if (!passwordResult.Succeeded)
                        {
                            var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
                            logger.LogError("Failed to reset admin password: {Errors}", errors);
                        }
                        else
                        {
                            logger.LogInformation("Admin password restored to seed default");
                        }
                    }
                    else
                    {
                        logger.LogInformation("Admin user already exists");
                    }
                }

                if (!await userManager.IsInRoleAsync(adminUser, AppRoleNames.SuperAdmin))
                    await userManager.AddToRoleAsync(adminUser, AppRoleNames.SuperAdmin);

                // Migrate legacy "Admin" role assignment if present.
                if (await userManager.IsInRoleAsync(adminUser, "Admin"))
                    await userManager.RemoveFromRoleAsync(adminUser, "Admin");

                if (!dbContext.Employees.Any(e => e.UserId == adminUser.Id))
                {
                    dbContext.Employees.Add(Employee.Create(adminUser.Id, "System Admin", "System"));
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding admin user");
            }
        }
    }
}
