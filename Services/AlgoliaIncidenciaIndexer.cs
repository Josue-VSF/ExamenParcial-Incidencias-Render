using Algolia.Search.Clients;
using Algolia.Search.Exceptions;
using Algolia.Search.Models.Search;
using ExamenParcial_Incidencias_Render.Models;
using Microsoft.Extensions.Options;

namespace ExamenParcial_Incidencias_Render.Services;

/// Escribe las incidencias de SQLite en el indice de Algolia y lo mantiene al
/// dia cuando una incidencia cambia. La lectura del indice vive en
/// IIncidenciaSearchService; aqui solo se escribe, para que ambos sentidos
/// del indice queden cubiertos.
///
/// Escribir exige una Admin API Key: la Search API Key que usa el navegador
/// solo tiene el permiso "search" y Algolia responde 403 (missing ACL
/// "addObject") si se intenta guardar. Sin Admin API Key la sincronizacion se
/// omite con un aviso y la app sigue funcionando.
public interface IIncidenciaIndexer
{
    /// Reemplaza el contenido del indice y aplica la configuracion de busqueda.
    Task SincronizarTodoAsync(IReadOnlyCollection<Incidencia> incidencias, CancellationToken cancellationToken = default);

    /// Actualiza un unico documento del indice.
    Task ActualizarAsync(Incidencia incidencia, CancellationToken cancellationToken = default);
}

public class AlgoliaIncidenciaIndexer : IIncidenciaIndexer
{
    private readonly AlgoliaOptions _opciones;
    private readonly ILogger<AlgoliaIncidenciaIndexer> _logger;
    private readonly Lazy<SearchClient> _clientEscritura;
    private int _yaAvisado;

    public AlgoliaIncidenciaIndexer(
        IOptions<AlgoliaOptions> opciones,
        ILoggerFactory loggerFactory,
        ILogger<AlgoliaIncidenciaIndexer> logger)
    {
        _opciones = opciones.Value;
        _logger = logger;
        _clientEscritura = new Lazy<SearchClient>(
            () => new SearchClient(_opciones.ApplicationId, _opciones.ClaveEscritura, loggerFactory),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public async Task SincronizarTodoAsync(
        IReadOnlyCollection<Incidencia> incidencias,
        CancellationToken cancellationToken = default)
    {
        if (!PrepararEscritura("sincronizar el indice"))
        {
            return;
        }

        try
        {
            await _clientEscritura.Value.SetSettingsAsync(
                _opciones.IndexName,
                new IndexSettings
                {
                    SearchableAttributes = ["Estacion", "Descripcion"],
                    AttributesForFaceting = ["filterOnly(Estado)"]
                },
                cancellationToken: cancellationToken);

            var documentos = incidencias
                .Select(IncidenciaAlgolia.Desde)
                .ToList();

            if (documentos.Count == 0)
            {
                await _clientEscritura.Value.ClearObjectsAsync(_opciones.IndexName, cancellationToken: cancellationToken);
                _logger.LogInformation("Indice Algolia {Indice} vaciado", _opciones.IndexName);
                return;
            }

            await _clientEscritura.Value.SaveObjectsAsync(
                _opciones.IndexName,
                documentos,
                waitForTasks: true,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Indice Algolia {Indice} sincronizado con {Cantidad} incidencia(s)",
                _opciones.IndexName,
                documentos.Count);
        }
        catch (AlgoliaApiException excepcion) when (EsFaltaDePermisos(excepcion))
        {
            AvisarPermisos(excepcion);
        }
        catch (Exception excepcion)
        {
            // El indice es una proyeccion: si Algolia falla, SQLite sigue siendo
            // la fuente de verdad y la app no debe caerse.
            _logger.LogError(excepcion, "No se pudo sincronizar el indice de Algolia");
        }
    }

    public async Task ActualizarAsync(Incidencia incidencia, CancellationToken cancellationToken = default)
    {
        if (!PrepararEscritura("actualizar el indice"))
        {
            return;
        }

        try
        {
            await _clientEscritura.Value.PartialUpdateObjectAsync(
                _opciones.IndexName,
                incidencia.Id.ToString(),
                IncidenciaAlgolia.Desde(incidencia),
                createIfNotExists: true,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Incidencia {Id} actualizada en el indice Algolia {Indice}",
                incidencia.Id,
                _opciones.IndexName);
        }
        catch (AlgoliaApiException excepcion) when (EsFaltaDePermisos(excepcion))
        {
            AvisarPermisos(excepcion);
        }
        catch (Exception excepcion)
        {
            _logger.LogError(excepcion, "No se pudo actualizar la incidencia {Id} en Algolia", incidencia.Id);
        }
    }

    /// Devuelve false cuando no se puede escribir, y en ese caso ya ha avisado.
    private bool PrepararEscritura(string operacion)
    {
        if (!_opciones.IsConfigured)
        {
            _logger.LogWarning("Algolia no esta configurado: se omite {Operacion}", operacion);
            return false;
        }

        if (!_opciones.PuedeEscribir)
        {
            if (Interlocked.Exchange(ref _yaAvisado, 1) == 0)
            {
                _logger.LogWarning(
                    "Algolia: se omite {Operacion} porque la clave configurada es de solo busqueda. " +
                    "Define 'Algolia:AdminApiKey' (Algolia > Settings > API Keys > Admin API Key) " +
                    "para poder crear y escribir en el indice {Indice}. " +
                    "Mientras tanto la busqueda consulta el indice tal cual este.",
                    operacion,
                    _opciones.IndexName);
            }

            return false;
        }

        return true;
    }

    private static bool EsFaltaDePermisos(AlgoliaApiException excepcion) =>
        excepcion.Message.Contains("403", StringComparison.Ordinal) ||
        excepcion.Message.Contains("missing ACL", StringComparison.Ordinal);

    private void AvisarPermisos(AlgoliaApiException excepcion)
    {
        if (Interlocked.Exchange(ref _yaAvisado, 1) == 0)
        {
            _logger.LogWarning(
                "Algolia rechazo la escritura con 403: {Motivo}. Revisa que 'Algolia:AdminApiKey' " +
                "sea una Admin API Key y que el indice {Indice} exista.",
                excepcion.Message,
                _opciones.IndexName);
        }
    }
}
