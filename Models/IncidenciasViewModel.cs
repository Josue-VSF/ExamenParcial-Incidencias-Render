namespace ExamenParcial_Incidencias_Render.Models;

public class IncidenciasViewModel
{
    public IEnumerable<Incidencia> Incidencias { get; set; } = Enumerable.Empty<Incidencia>();

    /// Configuracion de PieHost ya serializada a JSON para la vista.
    /// Se genera en el controlador porque System.Text.Json no esta disponible
    /// dentro de la compilacion de las vistas.
    public string ConfigJson { get; set; } = "{}";
}
