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
        logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
        logger.LogInformation("Database provider: {Provider}", dbContext.Database.ProviderName);
        logger.LogInformation("Using connection string: {ConnectionString}", connectionString);

        // Check database connection
        try
        {
            if (dbContext.Database.CanConnect())
            {
                logger.LogInformation("Successfully connected to database");
            }
            else
            {
                logger.LogWarning("Cannot connect to database. Will attempt to create it.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Error checking database connection: {Message}", ex.Message);
        }

        // Handle database creation/migration
        if (isDevelopment)
        {
            // For SQLite in development
            logger.LogInformation("Development environment: Using migrations for SQLite");
            dbContext.Database.Migrate();
        }
        else
        {
            // For PostgreSQL in production, use EnsureCreated for reliability
            logger.LogInformation("Production environment: Creating database schema directly");
            
            try
            {
                // First try EnsureCreated which is more reliable across provider changes
                dbContext.Database.EnsureCreated();
                logger.LogInformation("Database schema created successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create database schema");
                throw; // Re-throw to prevent app from starting with a broken database
            }
        }

        // Handle data seeding
        try
        {
            logger.LogInformation("Checking if data seeding is needed");
            
            // Try to query Authors table to see if it exists and has data
            bool hasData = false;
            
            try
            {
                // Simple check to see if we can get data
                hasData = dbContext.Authors.Any();
                logger.LogInformation("Authors table exists and has data: {HasData}", hasData);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Error checking Authors table: {Message}", ex.Message);
                logger.LogInformation("Assuming Authors table is empty or doesn't exist");
            }
            
            if (!hasData)
            {
                logger.LogInformation("Seeding initial data");
                ApplicationDbInitializer.Initialize(dbContext);
                logger.LogInformation("Data seeding completed successfully");
            }
            else
            {
                logger.LogInformation("Database already contains data, skipping seed");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during data seeding");
            // Continue application startup even if seeding fails
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed");
        // In production, this would prevent the app from starting with a broken database
        if (!isDevelopment)
        {
            throw; // Re-throw in production to prevent app from starting with DB issues
        }
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