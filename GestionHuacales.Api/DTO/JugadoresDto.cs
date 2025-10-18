using System.ComponentModel.DataAnnotations;

namespace GestionHuacales.Api.DTO
{
    public class JugadoresCreateDto
    {
        [Required(ErrorMessage = "El nombre del jugador es obligatorio")]
        [StringLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El nombre solo puede contener letras y espacios")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email del jugador es obligatorio")]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string Email { get; set; } = string.Empty;
    }

    public class JugadoresUpdateDto
    {
        [Required(ErrorMessage = "El nombre del jugador es obligatorio")]
        [StringLength(50, ErrorMessage = "El nombre no puede exceder 50 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "El nombre solo puede contener letras y espacios")]
        public string Nombres { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email del jugador es obligatorio")]
        [StringLength(100, ErrorMessage = "El email no puede exceder 100 caracteres")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string Email { get; set; } = string.Empty;
    }

    public class JugadoresResponseDto
    {
        public int JugadorId { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public int Victorias { get; set; }
        public int Derrotas { get; set; }
        public int Empates { get; set; }
        public int TotalPartidas => Victorias + Derrotas + Empates;
        public double PorcentajeVictorias => TotalPartidas > 0 ? (double)Victorias / TotalPartidas * 100 : 0;
    }

    public class JugadoresListDto
    {
        public int JugadorId { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Victorias { get; set; }
        public int Derrotas { get; set; }
        public int Empates { get; set; }
        public int TotalPartidas => Victorias + Derrotas + Empates;
        public double PorcentajeVictorias => TotalPartidas > 0 ? (double)Victorias / TotalPartidas * 100 : 0;
    }

    public class JugadoresStatsDto
    {
        public int JugadorId { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public int Victorias { get; set; }
        public int Derrotas { get; set; }
        public int Empates { get; set; }
        public int TotalPartidas => Victorias + Derrotas + Empates;
        public double PorcentajeVictorias => TotalPartidas > 0 ? (double)Victorias / TotalPartidas * 100 : 0;
        public double PorcentajeDerrotas => TotalPartidas > 0 ? (double)Derrotas / TotalPartidas * 100 : 0;
        public double PorcentajeEmpates => TotalPartidas > 0 ? (double)Empates / TotalPartidas * 100 : 0;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaUltimaPartida { get; set; }
    }
}