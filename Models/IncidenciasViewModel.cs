namespace ExamenParcial_Incidencias_Render.Models;

public class IncidenciasViewModel
{
    public List<Incidencia> Incidencias { get; set; } = new();

    public string Query { get; set; } = string.Empty;

    public bool ConBusqueda { get; set; }
}
