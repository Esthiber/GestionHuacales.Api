using GestionHuacales.Api.Models;
using GestionHuacales.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace GestionHuacales.Api.DTO;

public class PartidasCreateDto
{
    [Required(ErrorMessage = "El Jugador1Id es obligatorio")]
    public int Jugador1Id { get; set; }

    public int? Jugador2Id { get; set; }
}

public class PartidasUpdateDto
{
    public int? Jugador2Id { get; set; }
    
    public EstadoPartida? EstadoPartida { get; set; }
    
    public int? GanadorId { get; set; }
    
    public int? TurnoJugadorId { get; set; }
}

public class PartidasResponseDto
{
    public int PartidaId { get; set; }
    public int Jugador1Id { get; set; }
    public string NombreJugador1 { get; set; } = string.Empty;
    public int? Jugador2Id { get; set; }
    public string? NombreJugador2 { get; set; }
    public EstadoPartida EstadoPartida { get; set; }
    public string EstadoDescripcion { get; set; } = string.Empty;
    public int? GanadorId { get; set; }
    public string? NombreGanador { get; set; }
    public int TurnoJugadorId { get; set; }
    public string NombreTurnoJugador { get; set; } = string.Empty;
    public string EstadoTablero { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public bool EstaActiva { get; set; }
    public bool PuedeRecibirMovimientos { get; set; }
    public int CantidadMovimientos { get; set; }
}

public class PartidasListDto
{
    public int PartidaId { get; set; }
    public string NombreJugador1 { get; set; } = string.Empty;
    public string? NombreJugador2 { get; set; }
    public EstadoPartida EstadoPartida { get; set; }
    public string EstadoDescripcion { get; set; } = string.Empty;
    public string? NombreGanador { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public int CantidadMovimientos { get; set; }
}

public class PartidasJoinDto
{
    [Required(ErrorMessage = "El PartidaId es obligatorio")]
    public int PartidaId { get; set; }
    
    [Required(ErrorMessage = "El JugadorId es obligatorio")]
    public int JugadorId { get; set; }
}
