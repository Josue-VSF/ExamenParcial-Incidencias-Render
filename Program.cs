using ExamenParcial_Incidencias_Render.Data;
using ExamenParcial_Incidencias_Render.Models;
using ExamenParcial_Incidencias_Render.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Credenciales locales de desarrollo (archivo ignorado por git)
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Base de datos: SQLite mediante EF Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity con soporte de roles (Identity UI incluida para login/registro)
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// Busqueda en Algolia: las credenciales se leen de IConfiguration y solo viven en el servidor
builder.Services.Configure<AlgoliaOptions>(builder.Configuration.GetSection(AlgoliaOptions.SectionName));
builder.Services.AddScoped<IIncidenciaSearchService, AlgoliaIncidenciaSearchService>();

var app = builder.Build();

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

// Aplica migraciones y carga rol, usuario supervisor e incidencias de prueba
await app.Services.InitializeDatabaseAsync();

app.Run();
