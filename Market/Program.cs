using Market.Data;
using Market.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// services
builder.Services.AddScoped<IFileStorage, LocalFileStorage>();

var cs = builder.Configuration.GetConnectionString("DefaultConnection")
         ?? throw new InvalidOperationException("Missing connection string");

builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(cs));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<IdentityUser>(o =>
    {
        o.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// Forwarded headers (ważne za reverse proxy: Caddy/Nginx/Traefik)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Jeśli masz stały reverse proxy w tej samej sieci dockerowej, można ograniczyć KnownNetworks/Proxies.
    // Przy prostym wdrożeniu zostawiamy bez ograniczeń, żeby nie blokować nagłówków.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// init db + seed
await InitializeDatabaseAsync(app);

app.Run();


// helpers
static async Task InitializeDatabaseAsync(IHost appHost)
{
    using var scope = appHost.Services.CreateScope();
    var sp = scope.ServiceProvider;

    var db = sp.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    await SeedInitialData(sp);
}

static async Task SeedInitialData(IServiceProvider sp)
{
    var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = sp.GetRequiredService<UserManager<IdentityUser>>();
    var config = sp.GetRequiredService<IConfiguration>();

    // Role (bez polskich znaków; spójnie w całym projekcie)
    string[] roles = { "User", "Moderator", "Admin" };
    foreach (var r in roles)
    {
        if (!await roleManager.RoleExistsAsync(r))
            await roleManager.CreateAsync(new IdentityRole(r));
    }

    // Przypnij rolę bazową użytkownikom bez ról
    foreach (var u in userManager.Users.ToList())
    {
        if (!(await userManager.GetRolesAsync(u)).Any())
            await userManager.AddToRoleAsync(u, "User");
    }

    // Admin tylko, jeśli podano w konfiguracji (ENV/sekrety)
    // W docker-compose: Seed__AdminEmail / Seed__AdminPassword
    var adminEmail = config["Seed:AdminEmail"];
    var adminPassword = config["Seed:AdminPassword"];

    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        return;

    var admin = await userManager.FindByEmailAsync(adminEmail);
    if (admin is null)
    {
        var newAdmin = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var res = await userManager.CreateAsync(newAdmin, adminPassword);
        if (res.Succeeded)
            await userManager.AddToRoleAsync(newAdmin, "Admin");
    }
}
