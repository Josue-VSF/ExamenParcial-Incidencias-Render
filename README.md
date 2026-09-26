# Gestion de incidencias de bicicletas (examen parcial)

Aplicacion ASP.NET Core 8 MVC con las tres integraciones del examen:

| Pregunta | Tecnologia | Que hace |
| --- | --- | --- |
| 1 | **Algolia** | Indice de incidencias con busqueda por estacion y descripcion. El indice se sincroniza con SQLite en cada arranque y al cerrar una incidencia. |
| 2 | **Redis** | Cache distribuida del listado de incidencias abiertas durante 60 segundos, con invalidacion al cerrar. |
| 3 | **PieHost (PieSocket)** | WebSocket que publica el evento `IncidenciaActualizada` al cerrar una incidencia y actualiza la fila en los otros navegadores sin recargar. |
| 4 | **Render** | `Dockerfile` multi-etapa que arranca en el puerto que asigna Render. |

## Estructura

```
Controllers/     HomeController, OperacionesController (listado, busqueda, estado, cierre)
Services/        AlgoliaIncidenciaSearchService (lectura), AlgoliaIncidenciaIndexer (escritura),
                 PieSocketPublisher (publicacion de eventos)
Data/            ApplicationDbContext, ApplicationUser, DbInitializer, DatosPrueba
Models/          Incidencia, IncidenciasViewModel, AlgoliaOptions, PieSocketOptions
Migrations/       Migracion inicial de EF Core
Views/           Razor + Bootstrap 5
```

## Puesta en marcha local

1. Crear `appsettings.Local.json` en la raiz (esta en `.gitignore`, nunca se versiona):

```json
{
  "ConnectionStrings": {
    "RedisConnection": "host:puerto,password=..."
  },
  "Algolia": {
    "ApplicationId": "...",
    "ApiKey": "...",
    "AdminApiKey": "...",
    "IndexName": "incidencias"
  },
  "PieSocket": {
    "ApiKey": "...",
    "ApiSecret": "...",
    "ClusterId": "...",
    "RoomId": "..."
  }
}
```

### Admin API Key de Algolia (importante)

`ApiKey` es la clave de **solo busqueda**: es la que puede ir en el navegador y solo
tiene el permiso `search`. No puede crear ni escribir en el indice, Algolia responde

```
403 Method not allowed with this API key, missing ACL "addObject"
```

Para que la app **sincronice** el indice (que es lo que la P1 pide) hace falta la
**Admin API Key**: en Algolia, `Settings > API Keys > Admin API Key`, y copiala en
`AdminApiKey` (en Render: variable `Algolia__AdminApiKey`).

Sin ella la app no se rompe: avisa una vez en el log, omite la escritura y sigue
consultando el indice con la clave de busqueda. Si el indice no existe todavia, el
listado se muestra igual y la busqueda muestra el aviso de que Algolia no respondio.

2. `dotnet run`

El arranque aplica las migraciones, crea el rol `Supervisor`, siembra las incidencias de
prueba y sincroniza el indice de Algolia.

### Usuario de prueba

| Campo | Valor |
| --- | --- |
| Correo | `supervisor@test.com` |
| Contrasena | `Supervisor123!` |

Es el unico usuario con acceso a `/Operaciones/Incidencias`.

## Despliegue en Render

El repositorio ya trae `render.yaml`, asi que Render lo detecta como *Blueprint*.

1. **New > Blueprint** y apuntar al repositorio.
2. Completar las variables de entorno marcadas como `sync: false` (Render no puede inventarlas):
   - `ConnectionStrings__RedisConnection` (obligatoria, la app no arranca sin ella)
   - `Algolia__ApplicationId`, `Algolia__ApiKey`, `Algolia__AdminApiKey`, `Algolia__IndexName`
   - `PieSocket__ApiKey`, `PieSocket__ApiSecret`, `PieSocket__ClusterId`, `PieSocket__RoomId`
3. Deploy.

Si prefieres configurarlo a mano: **New > Web Service > Docker**, y las mismas variables
en *Environment*.

### Notas de despliegue

- **SQLite es efimero.** El contenedor arranca con el sistema de archivos limpio, asi que la
  base se crea y se siembra sola en cada despliegue y se pierde al redeploy. Para conservarla,
  monta un disco persistente y pon
  `ConnectionStrings__DefaultConnection = "Data Source=/var/data/incidencias.db"`.
- **Redis es obligatorio.** `Program.cs` lanza una excepcion clara si falta
  `ConnectionStrings__RedisConnection`, para no arrancar con el listado a medias.
- **PieHost y Algolia son opcionales en el arranque.** Si faltan sus credenciales, la app
  levanta igual: la busqueda avisa con un mensaje y el WebSocket muestra
  "PieHost sin configurar".
- **TLS** termina en el proxy de Render; `UseForwardedHeaders` esta configurado para que
  HSTS y la redireccion a HTTPS no rompan.
- **Free tier** apaga el servicio tras inactividad, asi que la primera carga puede tardar.

## Seguridad

- `appsettings.Local.json` esta en `.gitignore` y en `.dockerignore`, y el `.csproj` impide
  copiarlo a la salida de compilacion: los secretos nunca entran en la imagen.
- Las credenciales viajan como variables de entorno con el prefijo de seccion
  (`Algolia__ApiKey`, `PieSocket__ApiSecret`, ...).
- **Pendiente de rotar:** el commit `a5debfd` versiono la contrasena de Redis y la clave de
  Algolia antes de eliminarlas. Si el repositorio es publico, regenera ambas credenciales.
