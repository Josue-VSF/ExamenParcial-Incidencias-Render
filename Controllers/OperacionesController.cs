using System.Text.Json;
using ExamenParcial_Incidencias_Render.Data;
using ExamenParcial_Incidencias_Render.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace ExamenParcial_Incidencias_Render.Controllers;

[Authorize(Roles = DbInitializer.RolSupervisor)]
public class OperacionesController : Controller
{
    private const string CacheKeyIncidencias = "operaciones:incidencias-abiertas";

    private static readonly TimeSpan CacheExpiracion = TimeSpan.FromSeconds(60);

    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(
        ApplicationDbContext context,
        IDistributedCache cache,
        ILogger<OperacionesController> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IActionResult> Incidencias()
    {
        var jsonCacheado = await _cache.GetStringAsync(CacheKeyIncidencias);

        if (jsonCacheado is not null)
        {
            var desdeCache = JsonSerializer.Deserialize<List<Incidencia>>(jsonCacheado) ?? [];

            _logger.LogInformation(
                "CACHE HIT: listado de incidencias abiertas ({Cantidad}) leído desde Redis",
                desdeCache.Count);

            return View(desdeCache);
        }

        _logger.LogInformation(
            "CACHE MISS: no hay entrada en Redis, se lee el listado de incidencias abiertas desde SQLite");

        var incidencias = await _context.Incidencias
            .Where(incidencia => incidencia.Estado == EstadoIncidencia.Abierta)
            .OrderByDescending(incidencia => incidencia.Prioridad)
            .ToListAsync();

        await _cache.SetStringAsync(
            CacheKeyIncidencias,
            JsonSerializer.Serialize(incidencias),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiracion
            });

        _logger.LogInformation(
            "CACHE LLENADO: {Cantidad} incidencias guardadas en Redis con expiracion de {Segundos} segundos",
            incidencias.Count,
            CacheExpiracion.TotalSeconds);

        return View(incidencias);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id)
    {
        var incidencia = await _context.Incidencias.FindAsync(id);

        if (incidencia is null)
        {
            return NotFound();
        }

        incidencia.Estado = EstadoIncidencia.Cerrada;
        await _context.SaveChangesAsync();

        await _cache.RemoveAsync(CacheKeyIncidencias);

        _logger.LogInformation(
            "CACHE INVALIDADA: clave '{Clave}' eliminada de Redis tras cerrar la incidencia {Id}",
            CacheKeyIncidencias,
            id);

        return RedirectToAction(nameof(Incidencias));
    }
}
