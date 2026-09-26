using System.Text.Json.Serialization;

namespace ExamenParcial_Incidencias_Render.Models;

/// Payload del evento IncidenciaActualizada.
/// Los nombres se fijan con [JsonPropertyName] para que el JSON publicado
/// llegue al cliente exactamente como { "Id": 1, "Estado": "Cerrada" },
/// sin aplicar el nombrado camelCase del resto del sobre.
public class IncidenciaActualizada
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("Estado")]
    public string Estado { get; set; } = string.Empty;
}
