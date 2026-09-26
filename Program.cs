using ExamenParcial_Incidencias_Render.Data;
using ExamenParcial_Incidencias_Render.Models;
using ExamenParcial_Incidencias_Render.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Credenciales locales de desarrollo (archivo ignorado por git).
// En Render se inyectan como variables de entorno.
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

// WebSocket en tiempo real con PieHost (PieSocket).
// La API Key y el API Secret se leen de la configuracion; en appsettings.json
// quedan en blanco y los valores reales viven fuera del repositorio.
builder.Services.Configure<PieSocketOptions>(builder.Configuration.GetSection(PieSocketOptions.SectionName));
builder.Services.AddHttpClient<IPieSocketPublisher, PieSocketPublisher>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

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
