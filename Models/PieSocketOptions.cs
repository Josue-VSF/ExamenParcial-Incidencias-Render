namespace ExamenParcial_Incidencias_Render.Models;

/// Configuracion del servicio de WebSocket PieHost (PieSocket).
/// Los valores reales NUNCA se versionan: en appsettings.json quedan en blanco
/// y se inyectan con appsettings.Local.json, variables de entorno o User Secrets.
public class PieSocketOptions
{
    public const string SectionName = "PieSocket";

    public string ApiKey { get; set; } = string.Empty;

    public string ApiSecret { get; set; } = string.Empty;

    public string ClusterId { get; set; } = string.Empty;

    public string RoomId { get; set; } = string.Empty;

    /// URL del WebSocket. Si se deja vacia se construye con ClusterId y RoomId.
    /// Se lee de la configuracion para no fijar ninguna URL en el codigo.
    public string WebSocketUrl { get; set; } = string.Empty;

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(ApiSecret) &&
        !string.IsNullOrWhiteSpace(ClusterId) &&
        !string.IsNullOrWhiteSpace(RoomId);

    public string UrlPublicacion => $"https://{ClusterId}.piesocket.com/api/publish";

    /// URL que usa el navegador para suscribirse. Devuelve vacio si falta lo
    /// minimo para construirla, de modo que la vista pueda avisar que PieHost
    /// no esta configurado en vez de intentar conectarse a una URL invalida.
    public string UrlSuscripcion
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(WebSocketUrl))
            {
                return WebSocketUrl;
            }

            if (string.IsNullOrWhiteSpace(ClusterId) || string.IsNullOrWhiteSpace(RoomId))
            {
                return string.Empty;
            }

            return $"wss://{ClusterId}.piesocket.com/v4/{RoomId}?api_key={ApiKey}&notify_self=1";
        }
    }
}
