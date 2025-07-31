using Market.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>() // Dodanie obs³ugi ról
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();


// --- ZMIANY ZACZYNAJ¥ SIÊ TUTAJ ---

// Krok 1 & 2: Automatyczne migracje i inicjalizacja danych z mechanizmem ponawiania
await InitializeDatabaseAsync(app);

// --- ZMIANY KOÑCZ¥ SIÊ TUTAJ ---


app.Run();


// --- Funkcje pomocnicze ---

async Task InitializeDatabaseAsync(IHost app)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        var maxRetries = 10;
        var delay = TimeSpan.FromSeconds(5);

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                logger.LogInformation("Attempting to connect to the database and apply migrations... (Attempt {Attempt})", i + 1);
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully.");

                logger.LogInformation("Attempting to seed initial data...");
                await SeedInitialData(services);
                logger.LogInformation("Seeding completed successfully.");

                return; // Sukces, wychodzimy z funkcji
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "An error occurred during database initialization. Retrying in {Delay} seconds...", delay.TotalSeconds);
                await Task.Delay(delay);
            }
        }

        logger.LogError("Could not initialize the database after {MaxRetries} attempts.", maxRetries);
    }
}

async Task SeedInitialData(IServiceProvider services)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    // Tworzenie ról
    string[] roles = { "Admin", "Najemca", "Wynajmuj¹cy" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Tworzenie domyœlnego konta administratora
    var adminEmail = "admin@admin.com";
    var adminPassword = "Pa$$w0rd";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        var newAdmin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        var result = await userManager.CreateAsync(newAdmin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(newAdmin, "Admin");
        }
    }
}
