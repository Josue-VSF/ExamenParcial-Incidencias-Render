# ---------- Etapa de compilacion ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Se copia solo el .csproj para que la restauracion de paquetes quede cacheada
# y no se repita en cada build mientras las dependencias no cambien.
COPY ExamenParcial-Incidencias-Render.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish "ExamenParcial-Incidencias-Render.csproj" -c Release -o /app/publish

# ---------- Etapa de ejecucion ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

# Render asigna el puerto dinamico en la variable PORT y la expone en el servicio.
# El entrypoint escucha en ese puerto; el 8080 es el valor por defecto para uso local.
EXPOSE 8080

COPY --from=build /app/publish .

# La base SQLite se crea y se siembra en el propio arranque del contenedor
# (MigrateAsync + rol Supervisor + usuario + datos de prueba), por lo que
# /app debe ser escribible por el usuario de la aplicacion.
RUN chown -R app:app /app
USER app

# --urls tiene la precedencia mas alta en la configuracion de ASP.NET Core,
# por eso la variable PORT pisa cualquier valor de ASPNETCORE_URLS o ASPNETCORE_HTTP_PORTS.
ENTRYPOINT ["/bin/sh", "-c", "exec dotnet ExamenParcial-Incidencias-Render.dll --urls http://+:${PORT:-8080}"]
