using System.ComponentModel;

namespace GestionHuacales.Api.Enums
{
    public enum EstadoPartida
    {
        [Description("Iniciada")]
        Iniciada = 1,
        
        [Description("En Progreso")]
        EnProgreso = 2,
        
        [Description("Finalizada")]
        Finalizada = 3
    }
}