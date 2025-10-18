using GestionHuacales.Api.DAL;
using GestionHuacales.Api.DTO;
using GestionHuacales.Api.Models;
using GestionHuacales.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionHuacales.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovimientosController : ControllerBase
    {
        private readonly Contexto _context;
        private readonly PartidasService _partidasService;

        public MovimientosController(Contexto context, PartidasService partidasService)
        {
            _context = context;
            _partidasService = partidasService;
        }

        // GET: api/Movimientos/5
        [HttpGet("{partidaId}")]
        public async Task<ActionResult<MovimientosListDto[]>> GetMovimientos(int partidaId)
        {
            var movimientos = await _context.Movimientos
                .Include(m => m.Jugador)
                .Where(m => m.PartidaId == partidaId)
                .Select(m => new MovimientosListDto
                {
                    MovimientoId = m.MovimientoId,
                    NombreJugador = m.Jugador != null ? m.Jugador.Nombres : "Jugador desconocido",
                    PosicionFila = m.PosicionFila,
                    PosicionColumna = m.PosicionColumna,
                    FechaMovimiento = m.FechaMovimiento
                })
                .OrderBy(m => m.FechaMovimiento)
                .ToArrayAsync();

            return Ok(movimientos);
        }

        // POST: api/Movimientos
        [HttpPost]
        public async Task<ActionResult<MovimientosResponseDto>> PostMovimientos(MovimientosCreateDto movimientoDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Verificar la validez del movimiento y si termina la partida
            var resultadoValidacion = await _partidasService.CheckMovimiento(movimientoDto);

            if (!resultadoValidacion.EsValido)
            {
                return BadRequest(resultadoValidacion.Mensaje);
            }

            // Obtener información de la partida
            var partida = await _context.Partidas
                .FirstOrDefaultAsync(p => p.PartidaId == movimientoDto.PartidaId);

            if (partida == null)
            {
                return NotFound("La partida no existe");
            }

            // Determinar el jugador que hace el movimiento
            var jugadorId = movimientoDto.Jugador.Equals("X")
                ? partida.Jugador1Id
                : partida.Jugador2Id ?? 0;

            // Crear el movimiento
            var movimiento = new Movimientos
            {
                PartidaId = movimientoDto.PartidaId,
                JugadorId = jugadorId,
                PosicionFila = movimientoDto.PosicionFila,
                PosicionColumna = movimientoDto.PosicionColumna,
                FechaMovimiento = DateTime.UtcNow
            };

            _context.Movimientos.Add(movimiento);

            // Si el movimiento termina la partida, finalizarla
            if (resultadoValidacion.TerminaPartida)
            {
                if (resultadoValidacion.EsEmpate)
                {
                    // Finalizar con empate
                    await _partidasService.FinalizarPartidaAsync(movimientoDto.PartidaId, null);
                }
                else if (resultadoValidacion.GanadorId.HasValue)
                {
                    // Finalizar con ganador
                    await _partidasService.FinalizarPartidaAsync(movimientoDto.PartidaId, resultadoValidacion.GanadorId);
                }
            }
            else
            {
                // Solo cambiar turno si el juego continúa
                partida.TurnoJugadorId = jugadorId == partida.Jugador1Id
                    ? (partida.Jugador2Id ?? partida.Jugador1Id)
                    : partida.Jugador1Id;
            }

            // Actualizar el estado del tablero
            await _partidasService.ActualizarEstadoTablero(movimientoDto.PartidaId);

            await _context.SaveChangesAsync();

            // Cargar el jugador para la respuesta
            await _context.Entry(movimiento)
                .Reference(m => m.Jugador)
                .LoadAsync();

            var response = new MovimientosResponseDto
            {
                MovimientoId = movimiento.MovimientoId,
                PartidaId = movimiento.PartidaId,
                JugadorId = movimiento.JugadorId,
                NombreJugador = movimiento.Jugador?.Nombres ?? "Jugador desconocido",
                PosicionFila = movimiento.PosicionFila,
                PosicionColumna = movimiento.PosicionColumna,
                FechaMovimiento = movimiento.FechaMovimiento
            };

            // Agregar información sobre el fin del juego en la respuesta
            if (resultadoValidacion.TerminaPartida)
            {
                return Ok(new
                {
                    Movimiento = response,
                    PartidaTerminada = true,
                    EsEmpate = resultadoValidacion.EsEmpate,
                    GanadorId = resultadoValidacion.GanadorId,
                    Mensaje = resultadoValidacion.Mensaje
                });
            }

            return CreatedAtAction("GetMovimientos", new { partidaId = movimiento.PartidaId }, response);
        }

        // DELETE: api/Movimientos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMovimientos(int id)
        {
            var movimiento = await _context.Movimientos
                .Include(m => m.Partida)
                .FirstOrDefaultAsync(m => m.MovimientoId == id);

            if (movimiento == null)
            {
                return NotFound("El movimiento no existe");
            }

            // Solo se pueden eliminar movimientos de partidas que estén en progreso
            if (movimiento.Partida.EstadoPartida != Enums.EstadoPartida.EnProgreso)
            {
                return BadRequest("Solo se pueden eliminar movimientos de partidas en progreso");
            }

            // Verificar que sea el último movimiento (para funcionalidad "undo")
            var ultimoMovimiento = await _context.Movimientos
                .Where(m => m.PartidaId == movimiento.PartidaId)
                .OrderByDescending(m => m.FechaMovimiento)
                .FirstOrDefaultAsync();

            if (ultimoMovimiento?.MovimientoId != id)
            {
                return BadRequest("Solo se puede eliminar el último movimiento realizado");
            }

            _context.Movimientos.Remove(movimiento);

            // Restaurar el turno al jugador que hizo el movimiento eliminado
            var partida = movimiento.Partida;
            partida.TurnoJugadorId = movimiento.JugadorId;

            // Actualizar el estado del tablero
            await _partidasService.ActualizarEstadoTablero(partida.PartidaId);

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Movimiento eliminado exitosamente" });
        }
    }
}
