using ExamenParcial_Incidencias_Render.Models;

namespace ExamenParcial_Incidencias_Render.Data;

public static class DatosPrueba
{
    public static List<Incidencia> Crear() =>
    [
        new Incidencia
        {
            Estacion = "Plaza Mayor",
            Descripcion = "Bicicleta con el freno trasero inservible",
            Prioridad = Prioridad.Alta,
            Estado = EstadoIncidencia.Abierta
        },
        new Incidencia
        {
            Estacion = "Universidad",
            Descripcion = "Candado del anclaje roto, no se pueden devolver las bicicletas",
            Prioridad = Prioridad.Alta,
            Estado = EstadoIncidencia.Abierta
        },
        new Incidencia
        {
            Estacion = "Mercado",
            Descripcion = "Pedal derecho sustituido con pieza no original",
            Prioridad = Prioridad.Media,
            Estado = EstadoIncidencia.Abierta
        },
        new Incidencia
        {
            Estacion = "Parque Central",
            Descripcion = "Neumatico delantero desinflado y con corte",
            Prioridad = Prioridad.Media,
            Estado = EstadoIncidencia.Abierta
        },
        new Incidencia
        {
            Estacion = "Estacion Norte",
            Descripcion = "Foco LED de la bicicleta fundido, no se ve en el tunel",
            Prioridad = Prioridad.Baja,
            Estado = EstadoIncidencia.Abierta
        },
        new Incidencia
        {
            Estacion = "Rio",
            Descripcion = "Asiento con el cierre danado, se afloja durante el recorrido",
            Prioridad = Prioridad.Baja,
            Estado = EstadoIncidencia.Abierta
        },
        new Incidencia
        {
            Estacion = "Puente",
            Descripcion = "Bicicleta atascada en el anclaje y sin espacio disponible",
            Prioridad = Prioridad.Media,
            Estado = EstadoIncidencia.Cerrada
        },
        new Incidencia
        {
            Estacion = "Catedral",
            Descripcion = "Manillar con la altura desajustada en la bicicleta 14",
            Prioridad = Prioridad.Baja,
            Estado = EstadoIncidencia.Cerrada
        },
        new Incidencia
        {
            Estacion = "Aeropuerto",
            Descripcion = "Cambio de cadena y ajuste de frenos realizado por taller",
            Prioridad = Prioridad.Alta,
            Estado = EstadoIncidencia.Cerrada
        },
        new Incidencia
        {
            Estacion = "Estacion Sur",
            Descripcion = "Timbre de la bicicleta 22 sin sonido",
            Prioridad = Prioridad.Baja,
            Estado = EstadoIncidencia.Cerrada
        }
    ];
}
