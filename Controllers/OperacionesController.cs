using ExamenParcial_Incidencias_Render.Data;
using ExamenParcial_Incidencias_Render.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamenParcial_Incidencias_Render.Controllers;

[Authorize(Roles = DbInitializer.RolSupervisor)]
public class OperacionesController : Controller
{
    private readonly ApplicationDbContext _context;

    public OperacionesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Incidencias()
    {
        var incidencias = await _context.Incidencias
            .Where(incidencia => incidencia.Estado == EstadoIncidencia.Abierta)
            .OrderByDescending(incidencia => incidencia.Prioridad)
            .ToListAsync();

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

        return RedirectToAction(nameof(Incidencias));
    }
}
