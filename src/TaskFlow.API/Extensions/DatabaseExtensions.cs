using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.API.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations at startup (retrying while the database container is still booting).
    /// Skipped when the app is started by "dotnet ef" tooling or when SKIP_DB_MIGRATION=true.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        var entryAssembly = Assembly.GetEntryAssembly()?.GetName().Name;
        var runningUnderEfTools =
            string.Equals(entryAssembly, "ef", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(entryAssembly, "dotnet-ef", StringComparison.OrdinalIgnoreCase);
        var skipRequested = string.Equals(
            Environment.GetEnvironmentVariable("SKIP_DB_MIGRATION"), "true", StringComparison.OrdinalIgnoreCase);

        if (runningUnderEfTools || skipRequested)
        {
            return;
        }

        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Database");
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!db.Database.GetMigrations().Any())
        {
            logger.LogWarning(
                "No EF Core migrations were found, so the database schema was NOT created. Create one with: " +
                "dotnet ef migrations add InitialCreate --project src/TaskFlow.Infrastructure " +
                "--startup-project src/TaskFlow.API --output-dir Persistence/Migrations");
            return;
        }

        const int maxAttempts = 10;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                db.Database.Migrate();
                logger.LogInformation("Database migrations applied.");
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Database not ready (attempt {Attempt}/{Max}). Retrying in 5 seconds...", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }
}
