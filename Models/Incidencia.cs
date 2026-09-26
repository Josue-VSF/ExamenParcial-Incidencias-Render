namespace ExamenParcial_Incidencias_Render.Models;

public enum Prioridad
{
    Baja = 0,
    Media = 1,
    Alta = 2
}

public enum EstadoIncidencia
{
    Abierta = 0,
    Cerrada = 1
}

public class Incidencia
{
    public int Id { get; set; }

    public string Estacion { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public Prioridad Prioridad { get; set; }

    public EstadoIncidencia Estado { get; set; } = EstadoIncidencia.Abierta;
}
