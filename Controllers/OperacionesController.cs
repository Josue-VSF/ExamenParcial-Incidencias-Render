using System.Text.Json;
using ExamenParcial_Incidencias_Render.Data;
using ExamenParcial_Incidencias_Render.Models;
using ExamenParcial_Incidencias_Render.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace ExamenParcial_Incidencias_Render.Controllers;

[Authorize(Roles = DbInitializer.RolSupervisor)]
public class OperacionesController : Controller
{
    private const string CacheKeyIncidencias = "operaciones:incidencias-abiertas";

    private static readonly TimeSpan CacheExpiracion = TimeSpan.FromSeconds(60);

    private readonly ApplicationDbContext _context;
    private readonly IIncidenciaSearchService _buscador;
    private readonly IDistributedCache _cache;
    private readonly IPieSocketPublisher _pieHost;
    private readonly PieSocketOptions _pieHostOpciones;
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(
        ApplicationDbContext context,
        IIncidenciaSearchService buscador,
        IDistributedCache cache,
        IPieSocketPublisher pieHost,
        IOptions<PieSocketOptions> opciones,
        ILogger<OperacionesController> logger)
    {
        _context = context;
        _buscador = buscador;
        _cache = cache;
        _pieHost = pieHost;
        _pieHostOpciones = opciones.Value;
        _logger = logger;
    }

    public async Task<IActionResult> Incidencias(string? query)
    {
        var model = new IncidenciasViewModel
        {
            Query = query?.Trim() ?? string.Empty,
            ConfigJson = ConstruirConfigJsonPieHost()
        };

        // Con busqueda activa el listado se filtra contra Algolia y no se cachea:
        // la cache de Redis guarda el listado completo de incidencias abiertas.
        if (string.IsNullOrWhiteSpace(model.Query))
        {
            model.Incidencias = await ObtenerIncidenciasCacheadas();
            return View(model);
        }

        model.ConBusqueda = true;

        IQueryable<Incidencia> consulta = _context.Incidencias
            .Where(incidencia => incidencia.Estado == EstadoIncidencia.Abierta);

        try
        {
            var ids = await _buscador.BuscarIdsAsync(model.Query);

            consulta = ids.Count == 0
                ? consulta.Where(incidencia => false)
                : consulta.Where(incidencia => ids.Contains(incidencia.Id));
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "No se pudo ejecutar la busqueda en Algolia para '{Query}'", model.Query);
            TempData["ErrorBusqueda"] =
                "No se pudo consultar el indice de Algolia. Revisa la configuracion de la seccion 'Algolia'.";
            model.ConBusqueda = false;
        }

        model.Incidencias = await consulta
            .OrderByDescending(incidencia => incidencia.Prioridad)
            .ToListAsync();

        return View(model);
    }

    /// Listado de incidencias abiertas servido desde Redis durante 60 segundos.
    private async Task<List<Incidencia>> ObtenerIncidenciasCacheadas()
    {
        var jsonCacheado = await _cache.GetStringAsync(CacheKeyIncidencias);

        if (jsonCacheado is not null)
        {
            var desdeCache = JsonSerializer.Deserialize<List<Incidencia>>(jsonCacheado) ?? [];

            _logger.LogInformation(
                "CACHE HIT: listado de incidencias abiertas ({Cantidad}) leído desde Redis",
                desdeCache.Count);

            return desdeCache;
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

        return incidencias;
    }

    /// Datos que la vista necesita para abrir el WebSocket de PieHost.
    private string ConstruirConfigJsonPieHost() => JsonSerializer.Serialize(new
    {
        urlSuscripcion = _pieHostOpciones.UrlSuscripcion,
        clusterId = _pieHostOpciones.ClusterId,
        roomId = _pieHostOpciones.RoomId,
        estadoUrl = Url.Action(nameof(IncidenciasEstado)) ?? string.Empty
    });

    /// Estado vigente de las incidencias. La vista lo consulta al reconectar el
    /// WebSocket para ponerse al dia con los cambios que hubiera perdido.
    /// Devuelve la misma forma que el evento IncidenciaActualizada ({ Id, Estado })
    /// para que el JavaScript tenga un unico camino de actualizacion.
    [HttpGet]
    public async Task<IActionResult> IncidenciasEstado()
    {
        var incidencias = await _context.Incidencias
            .Where(incidencia => incidencia.Estado == EstadoIncidencia.Abierta)
            .OrderByDescending(incidencia => incidencia.Prioridad)
            .Select(incidencia => new IncidenciaActualizada
            {
                Id = incidencia.Id,
                Estado = incidencia.Estado.ToString()
            })
            .ToListAsync();

        return Json(incidencias);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id, string? query)
    {
        var incidencia = await _context.Incidencias.FindAsync(id);

        if (incidencia is null)
        {
            return NotFound();
        }

        // 1) Se guarda el estado en SQLite.
        incidencia.Estado = EstadoIncidencia.Cerrada;
        await _context.SaveChangesAsync();

        // 2) Se invalida la clave de Redis para que el listado se reconstruya.
        await _cache.RemoveAsync(CacheKeyIncidencias);

        _logger.LogInformation(
            "CACHE INVALIDADA: clave '{Clave}' eliminada de Redis tras cerrar la incidencia {Id}",
            CacheKeyIncidencias,
            id);

        // 3) Con la persistencia confirmada, se publica el evento en PieHost
        //    para que las demas vistas actualicen la fila sin recargar.
        await _pieHost.PublicarAsync(
            "IncidenciaActualizada",
            new IncidenciaActualizada
            {
                Id = incidencia.Id,
                Estado = incidencia.Estado.ToString()
            });

        return RedirectToAction(nameof(Incidencias), new { query });
    }
}
