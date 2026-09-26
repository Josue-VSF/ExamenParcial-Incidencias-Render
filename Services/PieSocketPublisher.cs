using System.Net.Http.Json;
using System.Text.Json;
using ExamenParcial_Incidencias_Render.Models;
using Microsoft.Extensions.Options;

namespace ExamenParcial_Incidencias_Render.Services;

public interface IPieSocketPublisher
{
    Task PublicarAsync(string evento, object datos, CancellationToken cancellationToken = default);
}

/// Publica eventos en un canal de PieHost (PieSocket) mediante su Cluster API:
/// POST https://{ClusterId}.piesocket.com/api/publish
/// Cuerpo: { key, secret, roomId, message: { event, data } }
public class PieSocketPublisher : IPieSocketPublisher
{
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly PieSocketOptions _opciones;
    private readonly ILogger<PieSocketPublisher> _logger;

    public PieSocketPublisher(
        HttpClient httpClient,
        IOptions<PieSocketOptions> opciones,
        ILogger<PieSocketPublisher> logger)
    {
        _httpClient = httpClient;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public async Task PublicarAsync(string evento, object datos, CancellationToken cancellationToken = default)
    {
        if (!_opciones.EstaConfigurado)
        {
            _logger.LogWarning(
                "PieSocket no esta configurado (revise la seccion {Seccion} de appsettings.json): se omite el evento {Evento}",
                PieSocketOptions.SectionName,
                evento);
            return;
        }

        var sobre = new
        {
            key = _opciones.ApiKey,
            secret = _opciones.ApiSecret,
            roomId = _opciones.RoomId,
            message = new { @event = evento, data = datos }
        };

        try
        {
            using var respuesta = await _httpClient.PostAsJsonAsync(
                _opciones.UrlPublicacion, sobre, OpcionesJson, cancellationToken);

            if (respuesta.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Evento {Evento} publicado en PieHost sobre el canal {Canal}", evento, _opciones.RoomId);
            }
            else
            {
                var cuerpo = await respuesta.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "PieHost respondio {Codigo} al publicar {Evento}: {Cuerpo}",
                    (int)respuesta.StatusCode, evento, cuerpo);
            }
        }
        catch (Exception ex)
        {
            // La incidencia ya quedo guardada en SQLite: un fallo de publicacion
            // no debe revertir el cierre ni devolver un error al supervisor.
            _logger.LogError(ex, "No se pudo publicar {Evento} en PieHost", evento);
        }
    }
}
