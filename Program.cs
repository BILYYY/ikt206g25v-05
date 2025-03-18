using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Example.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Environment.IsDevelopment()
    ? builder.Configuration.GetConnectionString("DefaultConnection")
    : builder.Configuration.GetConnectionString("ProductionConnection");

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}

builder.Services.AddDefaultIdentity<IdentityUser>(options => 
        options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (app.Environment.IsDevelopment())
    {
        dbContext.Database.EnsureCreated();
        ApplicationDbInitializer.Initialize(dbContext);
    }
    else
    {
        dbContext.Database.Migrate();
        ApplicationDbInitializer.Initialize(dbContext);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
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