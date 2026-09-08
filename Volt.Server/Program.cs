using Volt.Server;
using Infrastructure.Data;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

var app = builder
    .ConfigureServices()
    .ConfigurePipeline();

// Apply pending migrations before seeding
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
    try
    {
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        // Log error but don't stop application startup
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "An error occurred while applying migrations. The database may not be available.");
    }
}

await SeedData.SeedAdminUserAsync(app.Services);

app.Run();
