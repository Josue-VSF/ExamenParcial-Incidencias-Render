namespace ExamenParcial_Incidencias_Render.Models;

public class IncidenciasViewModel
{
    public List<Incidencia> Incidencias { get; set; } = new();

    // Busqueda con Algolia (Pregunta 1)
    public string Query { get; set; } = string.Empty;

    public bool ConBusqueda { get; set; }

    /// Configuracion de PieHost ya serializada a JSON para la vista.
    /// Se genera en el controlador porque System.Text.Json no esta disponible
    /// dentro de la compilacion de las vistas.
    public string ConfigJson { get; set; } = "{}";
}
