# ---------- Etapa de compilacion ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solo el proyecto y restaurar primero para aprovechar la cache de capas
COPY ExamenParcial-Incidencias-Render.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish "ExamenParcial-Incidencias-Render.csproj" -c Release -o /app/publish

# ---------- Etapa de ejecucion ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Render asigna el puerto 8080 por defecto
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ExamenParcial-Incidencias-Render.dll"]
