namespace ExamenParcial_Incidencias_Render.Models;

public class AlgoliaOptions
{
    public const string SectionName = "Algolia";

    public string ApplicationId { get; set; } = string.Empty;

    /// Clave de solo busqueda. Es la que usa la web para consultar el indice.
    public string ApiKey { get; set; } = string.Empty;

    /// Clave de administracion del indice. Solo se usa en el servidor para
    /// escribir: la Search API Key no tiene los permisos addObject ni
    /// editSettings, asi que sin esta clave la app puede buscar pero no
    /// sincronizar. Se genera en Algolia > Settings > API Keys > Admin.
    public string AdminApiKey { get; set; } = string.Empty;

    public string IndexName { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApplicationId) &&
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(IndexName);

    /// Solo con Admin API Key se puede crear el indice y escribir en el.
    public bool PuedeEscribir => IsConfigured && !string.IsNullOrWhiteSpace(AdminApiKey);

    /// Clave a usar para escribir: la de administracion si esta definida.
    public string ClaveEscritura => string.IsNullOrWhiteSpace(AdminApiKey) ? ApiKey : AdminApiKey;
}

/// Documento que se guarda en el indice de Algolia. El objectID es el Id de la
/// incidencia, de modo que la busqueda devuelve ids que se filtran despues en
/// SQLite sin depender de la moneda del indice.
public class IncidenciaAlgolia
{
    public string ObjectID { get; set; } = string.Empty;

    public int Id { get; set; }

    public string Estacion { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Prioridad { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public static IncidenciaAlgolia Desde(Incidencia incidencia) => new()
    {
        ObjectID = incidencia.Id.ToString(),
        Id = incidencia.Id,
        Estacion = incidencia.Estacion,
        Descripcion = incidencia.Descripcion,
        Prioridad = incidencia.Prioridad.ToString(),
        Estado = incidencia.Estado.ToString()
    };
}
