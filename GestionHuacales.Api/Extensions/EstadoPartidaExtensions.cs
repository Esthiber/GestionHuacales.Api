using System.ComponentModel;
using System.Reflection;
using GestionHuacales.Api.Enums;

namespace GestionHuacales.Api.Extensions
{
    public static class EstadoPartidaExtensions
    {
        
        public static string GetDescription(this EstadoPartida estado)
        {
            var field = estado.GetType().GetField(estado.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? estado.ToString();
        }
       
        public static EstadoPartida ToEstadoPartida(this string estadoString)
        {
            if (string.IsNullOrWhiteSpace(estadoString))
                return EstadoPartida.Iniciada;

            return estadoString.ToLower().Trim() switch
            {
                "iniciada" => EstadoPartida.Iniciada,
                "en progreso" => EstadoPartida.EnProgreso,
                "enprogreso" => EstadoPartida.EnProgreso,
                "finalizada" => EstadoPartida.Finalizada,
                "activa" => EstadoPartida.EnProgreso, 
                "terminada" => EstadoPartida.Finalizada,
                "completada" => EstadoPartida.Finalizada,
                _ => EstadoPartida.Iniciada
            };
        }
     
        public static bool EstaActiva(this EstadoPartida estado)
        {
            return estado != EstadoPartida.Finalizada;
        }

        public static bool PuedeRecibirMovimientos(this EstadoPartida estado)
        {
            return estado == EstadoPartida.EnProgreso;
        }

      
        public static EstadoPartida SiguienteEstado(this EstadoPartida estado)
        {
            return estado switch
            {
                EstadoPartida.Iniciada => EstadoPartida.EnProgreso,
                EstadoPartida.EnProgreso => EstadoPartida.Finalizada,
                EstadoPartida.Finalizada => EstadoPartida.Finalizada,
                _ => EstadoPartida.Iniciada
            };
        }

        public static bool EsTransicionValida(this EstadoPartida estadoActual, EstadoPartida nuevoEstado)
        {
            return (estadoActual, nuevoEstado) switch
            {
                (EstadoPartida.Iniciada, EstadoPartida.EnProgreso) => true,
                (EstadoPartida.Iniciada, EstadoPartida.Finalizada) => true,
                (EstadoPartida.EnProgreso, EstadoPartida.Finalizada) => true,
                (EstadoPartida.Finalizada, _) => false,
                _ when estadoActual == nuevoEstado => true,
                _ => false
            };
        }

        public static IEnumerable<(EstadoPartida Estado, string Descripcion)> ObtenerTodosLosEstados()
        {
            return Enum.GetValues<EstadoPartida>()
                .Select(e => (e, e.GetDescription()));
        }
    }
}