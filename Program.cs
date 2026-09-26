using ExamenParcial_Incidencias_Render.Data;
using ExamenParcial_Incidencias_Render.Models;
using ExamenParcial_Incidencias_Render.Services;
using Microsoft.AspNetCore.HttpOverrides;
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

// Busqueda en Algolia: las credenciales se leen de IConfiguration y solo viven en el servidor
builder.Services.Configure<AlgoliaOptions>(builder.Configuration.GetSection(AlgoliaOptions.SectionName));
builder.Services.AddScoped<IIncidenciaSearchService, AlgoliaIncidenciaSearchService>();
builder.Services.AddScoped<IIncidenciaIndexer, AlgoliaIncidenciaIndexer>();

// Cache distribuida con Redis para el listado de incidencias
var redisConnection = builder.Configuration.GetConnectionString("RedisConnection");
if (string.IsNullOrWhiteSpace(redisConnection))
{
    throw new InvalidOperationException(
        "Connection string 'RedisConnection' not found. Definala en appsettings.Local.json " +
        "(solo en local) o mediante la variable de entorno ConnectionStrings__RedisConnection (Render).");
}

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName = "ExamenParcialIncidencias:";
});

// WebSocket en tiempo real con PieHost (PieSocket).
// La API Key y el API Secret se leen de la configuracion; en appsettings.json
// quedan en blanco y los valores reales viven fuera del repositorio.
builder.Services.Configure<PieSocketOptions>(builder.Configuration.GetSection(PieSocketOptions.SectionName));
builder.Services.AddHttpClient<IPieSocketPublisher, PieSocketPublisher>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// En Render el TLS termina en su proxy y el contenedor recibe HTTP plano.
// Sin esto, la app cree que la peticion no es segura y rompe HSTS y la
// redireccion a HTTPS. Se confia en los encabezados que inyecta el proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Debe ir antes de UseHsts / UseHttpsRedirection para que lean el protocolo real.
app.UseForwardedHeaders();

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
