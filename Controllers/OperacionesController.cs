using ExamenParcial_Incidencias_Render.Data;
using ExamenParcial_Incidencias_Render.Models;
using ExamenParcial_Incidencias_Render.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ExamenParcial_Incidencias_Render.Controllers;

[Authorize(Roles = DbInitializer.RolSupervisor)]
public class OperacionesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPieSocketPublisher _pieHost;
    private readonly PieSocketOptions _pieHostOpciones;

    public OperacionesController(
        ApplicationDbContext context,
        IPieSocketPublisher pieHost,
        IOptions<PieSocketOptions> opciones)
    {
        _context = context;
        _pieHost = pieHost;
        _pieHostOpciones = opciones.Value;
    }

    public async Task<IActionResult> Incidencias()
    {
        var incidencias = await _context.Incidencias
            .Where(incidencia => incidencia.Estado == EstadoIncidencia.Abierta)
            .OrderByDescending(incidencia => incidencia.Prioridad)
            .ToListAsync();

        var modelo = new IncidenciasViewModel
        {
            Incidencias = incidencias,
            ConfigJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                urlSuscripcion = _pieHostOpciones.UrlSuscripcion,
                clusterId = _pieHostOpciones.ClusterId,
                roomId = _pieHostOpciones.RoomId,
                estadoUrl = Url.Action(nameof(IncidenciasEstado)) ?? string.Empty
            })
        };

        return View(modelo);
    }

    /// Estado vigente de las incidencias. La vista lo consulta al reconectar el
    /// WebSocket para ponerse al dia con los cambios que pudiera haber perdido.
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
    public async Task<IActionResult> Cerrar(int id)
    {
        var incidencia = await _context.Incidencias.FindAsync(id);

        if (incidencia is null)
        {
            return NotFound();
        }

        // 1) Se guarda el estado en SQLite.
        incidencia.Estado = EstadoIncidencia.Cerrada;
        await _context.SaveChangesAsync();

        // 2) Con la persistencia confirmada, se publica el evento en PieHost
        //    para que las demas vistas actualicen la fila sin recargar.
        await _pieHost.PublicarAsync(
            "IncidenciaActualizada",
            new IncidenciaActualizada
            {
                Id = incidencia.Id,
                Estado = incidencia.Estado.ToString()
            });

        return RedirectToAction(nameof(Incidencias));
    }
}
