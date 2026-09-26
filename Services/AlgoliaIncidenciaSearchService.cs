using Algolia.Search.Clients;
using Algolia.Search.Models.Search;
using ExamenParcial_Incidencias_Render.Models;
using Microsoft.Extensions.Options;

namespace ExamenParcial_Incidencias_Render.Services;

public interface IIncidenciaSearchService
{
    bool IsConfigured { get; }

    Task<IReadOnlyList<int>> BuscarIdsAsync(string termino, CancellationToken cancellationToken = default);
}

public class AlgoliaIncidenciaSearchService : IIncidenciaSearchService
{
    private const int MaxHits = 100;

    private readonly AlgoliaOptions _opciones;
    private readonly ILogger<AlgoliaIncidenciaSearchService> _logger;
    private readonly Lazy<SearchClient> _client;

    public AlgoliaIncidenciaSearchService(
        IOptions<AlgoliaOptions> opciones,
        ILoggerFactory loggerFactory,
        ILogger<AlgoliaIncidenciaSearchService> logger)
    {
        _opciones = opciones.Value;
        _logger = logger;
        _client = new Lazy<SearchClient>(
            () => new SearchClient(_opciones.ApplicationId, _opciones.ApiKey, loggerFactory),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public bool IsConfigured => _opciones.IsConfigured;

    public async Task<IReadOnlyList<int>> BuscarIdsAsync(string termino, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "Algolia no esta configurado. Revisa la seccion 'Algolia' de appsettings.json o las variables de entorno.");
        }

        var consulta = new SearchForHits(_opciones.IndexName)
        {
            Query = termino,
            HitsPerPage = MaxHits,
            RestrictSearchableAttributes = ["Estacion", "Descripcion"],
            AttributesToRetrieve = ["objectID"]
        };

        var respuestas = await _client.Value
            .SearchForHitsAsync<Hit>([consulta], cancellationToken: cancellationToken);

        var ids = new List<int>();

        foreach (var hit in respuestas.Count > 0 ? respuestas[0].Hits : [])
        {
            if (int.TryParse(hit.ObjectID, out var id))
            {
                ids.Add(id);
            }
            else
            {
                _logger.LogWarning("El indice Algolia devolvio un objectID no numerico: {ObjectID}", hit.ObjectID);
            }
        }

        _logger.LogInformation("Busqueda Algolia '{Termino}': {Cantidad} ids devueltos", termino, ids.Count);

        return ids;
    }
}
