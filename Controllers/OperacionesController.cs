using ExamenParcial_Incidencias_Render.Data;
using ExamenParcial_Incidencias_Render.Models;
using ExamenParcial_Incidencias_Render.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamenParcial_Incidencias_Render.Controllers;

[Authorize(Roles = DbInitializer.RolSupervisor)]
public class OperacionesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IIncidenciaSearchService _buscador;
    private readonly ILogger<OperacionesController> _logger;

    public OperacionesController(
        ApplicationDbContext context,
        IIncidenciaSearchService buscador,
        ILogger<OperacionesController> logger)
    {
        _context = context;
        _buscador = buscador;
        _logger = logger;
    }

    public async Task<IActionResult> Incidencias(string? query)
    {
        var model = new IncidenciasViewModel
        {
            Query = query?.Trim() ?? string.Empty
        };

        var busquedaActiva = !string.IsNullOrWhiteSpace(model.Query);

        IQueryable<Incidencia> consulta = _context.Incidencias
            .Where(incidencia => incidencia.Estado == EstadoIncidencia.Abierta);

        if (busquedaActiva)
        {
            model.ConBusqueda = true;

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
        }

        model.Incidencias = await consulta
            .OrderByDescending(incidencia => incidencia.Prioridad)
            .ToListAsync();

        return View(model);
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

        incidencia.Estado = EstadoIncidencia.Cerrada;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Incidencias), new { query });
    }
}
