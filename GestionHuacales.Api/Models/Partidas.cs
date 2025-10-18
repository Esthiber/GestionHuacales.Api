using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GestionHuacales.Api.Models;
using GestionHuacales.Api.Enums;
using GestionHuacales.Api.Extensions;

namespace GestionHuacales.Api.Models;

public class Partidas
{
    [Key]
    public int PartidaId { get; set; }

    public int Jugador1Id { get; set; } = 0;

    public int? Jugador2Id { get; set; } = null;

    [Required]
    public EstadoPartida EstadoPartida { get; set; } = EstadoPartida.Iniciada;

    public int? GanadorId { get; set; } = null;

    public int TurnoJugadorId { get; set; }

    public string? EstadoTablero { get; set; }

    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFin { get; set; }

    [ForeignKey(nameof(Jugador1Id))]
    public virtual Jugadores? Jugador1 { get; set; }

    [ForeignKey(nameof(Jugador2Id))]
    public virtual Jugadores? Jugador2 { get; set; }

    [ForeignKey(nameof(GanadorId))]
    public virtual Jugadores? Ganador { get; set; }

    [ForeignKey(nameof(TurnoJugadorId))]
    public virtual Jugadores? TurnoJugador { get; set; }

    public virtual ICollection<Movimientos> Movimientos { get; set; } = new List<Movimientos>();

}