using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Example.Data;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Determine environment and connection string
var isDevelopment = builder.Environment.IsDevelopment();
var connectionString = isDevelopment
    ? builder.Configuration.GetConnectionString("DefaultConnection")
    : builder.Configuration.GetConnectionString("ProductionConnection");

// Configure database context based on environment
if (isDevelopment)
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}

// Add Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
        options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Apply database migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation($"Environment: {app.Environment.EnvironmentName}");
        logger.LogInformation($"Using connection string: {connectionString}");
        logger.LogInformation($"Database Provider: {(isDevelopment ? "SQLite" : "PostgreSQL")}");

        // Check database connection
        if (dbContext.Database.CanConnect())
        {
            logger.LogInformation("Successfully connected to database");
        }
        else
        {
            logger.LogWarning("Cannot connect to database. Trying to create it...");
        }

        // Apply migrations or create database
        if (isDevelopment)
        {
            // For SQLite, migrations are safer
            logger.LogInformation("Applying migrations for SQLite...");
            dbContext.Database.Migrate();
        }
        else
        {
            try
            {
                // For PostgreSQL in production, first try migrations
                logger.LogInformation("Attempting to apply PostgreSQL migrations...");
                dbContext.Database.Migrate();
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Migration failed: {ex.Message}");
                logger.LogInformation("Falling back to EnsureCreated for PostgreSQL...");
                
                // If migrations fail, try to create the database directly
                dbContext.Database.EnsureCreated();
            }
        }

        // Seed the database if empty
        try
        {
            logger.LogInformation("Checking if data seeding is needed...");
            // Use a direct SQL query to check if the Authors table exists and has data
            var tableExists = false;
            
            if (isDevelopment)
            {
                // SQLite approach
                var result = dbContext.Database.ExecuteSqlRaw(
                    "SELECT name FROM sqlite_master WHERE type='table' AND name='Authors'");
                tableExists = result > 0;
            }
            else
            {
                // PostgreSQL approach
                var result = dbContext.Database.ExecuteSqlRaw(
                    "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'Authors')");
                tableExists = result > 0;
            }

            if (!tableExists || !dbContext.Authors.Any())
            {
                logger.LogInformation("Seeding initial data...");
                ApplicationDbInitializer.Initialize(dbContext);
            }
            else
            {
                logger.LogInformation("Database already contains data, skipping seed.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during data seeding");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database initialization");
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
//llgsdsa