namespace ExamenParcial_Incidencias_Render.Models;

/// Configuracion del servicio de WebSocket PieHost (PieSocket).
/// Los valores reales NUNCA se versionan: en appsettings.json quedan en blanco
/// y se inyectan con appsettings.Local.json, variables de entorno o User Secrets.
public class PieSocketOptions
{
    public const string SectionName = "PieSocket";

    /// Version del endpoint WebSocket. La documentada y la que enruta los
    /// mensajes es la v3: la v4 acepta el handshake y responde 200 al publish,
    /// pero los suscriptores no reciben nada, asi que fallaria en silencio.
    public const string VersionWebSocket = "v3";

    public string ApiKey { get; set; } = string.Empty;

    public string ApiSecret { get; set; } = string.Empty;

    public string ClusterId { get; set; } = string.Empty;

    public string RoomId { get; set; } = string.Empty;

    /// API ID del proyecto. Solo lo usan los SDK compatibles con Pusher; la API
    /// nativa de PieSocket (REST + WebSocket) no lo necesita. Se conserva para
    /// tener la referencia completa de las credenciales del cluster.
    public string ApiId { get; set; } = string.Empty;

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
    ///
    /// Solo lleva la ApiKey publica: el ApiSecret nunca sale del servidor.
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

            return $"wss://{ClusterId}.piesocket.com/{VersionWebSocket}/{RoomId}?api_key={ApiKey}&notify_self=1";
        }
    }
}
