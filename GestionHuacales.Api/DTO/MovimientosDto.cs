using System.ComponentModel.DataAnnotations;

namespace GestionHuacales.Api.DTO
{
    public class MovimientosCreateDto
    {
        [Required(ErrorMessage = "El PartidaId es obligatorio")]
        public int PartidaId { get; set; }

        [Required(ErrorMessage = "El jugador es obligatorio")]
        [RegularExpression("^[XO]$", ErrorMessage = "El jugador debe ser 'X' o 'O'")]
        public string Jugador { get; set; } = "X";

        [Required(ErrorMessage = "La posicion de fila es obligatoria")]
        [Range(0, 2, ErrorMessage = "La posicion de fila debe estar entre 0 y 2")]
        public int PosicionFila { get; set; }

        [Required(ErrorMessage = "La posicion de columna es obligatoria")]
        [Range(0, 2, ErrorMessage = "La posicion de columna debe estar entre 0 y 2")]
        public int PosicionColumna { get; set; }
    }

    public class MovimientosResponseDto
    {
        public int MovimientoId { get; set; }
        public int PartidaId { get; set; }
        public int JugadorId { get; set; }
        public string NombreJugador { get; set; } = string.Empty;
        public int PosicionFila { get; set; }
        public int PosicionColumna { get; set; }
        public DateTime FechaMovimiento { get; set; }
    }

    public class MovimientosListDto
    {
        public int MovimientoId { get; set; }
        public string NombreJugador { get; set; } = string.Empty;
        public int PosicionFila { get; set; }
        public int PosicionColumna { get; set; }
        public DateTime FechaMovimiento { get; set; }
    }
}
