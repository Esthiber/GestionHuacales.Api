using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestionHuacales.Api.DAL;
using GestionHuacales.Api.Models;
using GestionHuacales.Api.DTO;
using GestionHuacales.Api.Enums;
using GestionHuacales.Api.Extensions;
using GestionHuacales.Api.Services;

namespace GestionHuacales.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PartidasController : ControllerBase
    {
        private readonly Contexto _context;
        private readonly PartidasService _partidasService;

        public PartidasController(Contexto context, PartidasService partidasService)
        {
            _context = context;
            _partidasService = partidasService;
        }

        // GET: api/Partidas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PartidasListDto>>> GetPartidas([FromQuery] EstadoPartida? estado = null)
        {
            var query = _context.Partidas
                .Include(p => p.Jugador1)
                .Include(p => p.Jugador2)
                .Include(p => p.Ganador)
                .Include(p => p.Movimientos)
                .AsQueryable();

            if (estado.HasValue)
            {
                query = query.Where(p => p.EstadoPartida == estado.Value);
            }

            var partidas = await query
                .Select(p => new PartidasListDto
                {
                    PartidaId = p.PartidaId,
                    NombreJugador1 = p.Jugador1 != null ? p.Jugador1.Nombres : "Jugador no encontrado",
                    NombreJugador2 = p.Jugador2 != null ? p.Jugador2.Nombres : null,
                    EstadoPartida = p.EstadoPartida,
                    EstadoDescripcion = p.EstadoPartida.GetDescription(),
                    NombreGanador = p.Ganador != null ? p.Ganador.Nombres : null,
                    FechaInicio = p.FechaInicio,
                    FechaFin = p.FechaFin,
                    CantidadMovimientos = p.Movimientos.Count()
                })
                .OrderByDescending(p => p.FechaInicio)
                .ToListAsync();

            return Ok(partidas);
        }

        // GET: api/Partidas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PartidasResponseDto>> GetPartidas(int id)
        {
            var partida = await _context.Partidas
                .Include(p => p.Jugador1)
                .Include(p => p.Jugador2)
                .Include(p => p.Ganador)
                .Include(p => p.TurnoJugador)
                .Include(p => p.Movimientos)
                .Where(p => p.PartidaId == id)
                .Select(p => new PartidasResponseDto
                {
                    PartidaId = p.PartidaId,
                    Jugador1Id = p.Jugador1Id,
                    NombreJugador1 = p.Jugador1 != null ? p.Jugador1.Nombres : "Jugador no encontrado",
                    Jugador2Id = p.Jugador2Id,
                    NombreJugador2 = p.Jugador2 != null ? p.Jugador2.Nombres : null,
                    EstadoPartida = p.EstadoPartida,
                    EstadoDescripcion = p.EstadoPartida.GetDescription(),
                    GanadorId = p.GanadorId,
                    NombreGanador = p.Ganador != null ? p.Ganador.Nombres : null,
                    TurnoJugadorId = p.TurnoJugadorId,
                    NombreTurnoJugador = p.TurnoJugador != null ? p.TurnoJugador.Nombres : "Jugador no encontrado",
                    EstadoTablero = p.EstadoTablero,
                    FechaInicio = p.FechaInicio,
                    FechaFin = p.FechaFin,
                    EstaActiva = p.EstadoPartida.EstaActiva(),
                    PuedeRecibirMovimientos = p.EstadoPartida.PuedeRecibirMovimientos(),
                    CantidadMovimientos = p.Movimientos.Count()
                })
                .FirstOrDefaultAsync();

            if (partida == null)
            {
                return NotFound($"No se encontro la partida con ID {id}");
            }

            return Ok(partida);
        }

        // GET: api/Partidas/estados
        [HttpGet("estados")]
        public ActionResult<IEnumerable<object>> GetEstados()
        {
            var estados = EstadoPartidaExtensions.ObtenerTodosLosEstados()
                .Select(e => new { 
                    Valor = (int)e.Estado,
                    Nombre = e.Estado.ToString(),
                    Descripcion = e.Descripcion 
                });

            return Ok(estados);
        }

        // PUT: api/Partidas/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPartidas(int id, PartidasUpdateDto partidaDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var partida = await _context.Partidas.FindAsync(id);
            if (partida == null)
            {
                return NotFound($"No se encontró la partida con ID {id}");
            }

            if (partidaDto.Jugador2Id.HasValue)
            {
                partida.Jugador2Id = partidaDto.Jugador2Id;
            }

            if (partidaDto.TurnoJugadorId.HasValue)
            {
                partida.TurnoJugadorId = partidaDto.TurnoJugadorId.Value;
            }

            if (partidaDto.EstadoPartida.HasValue)
            {
                var exito = await _partidasService.CambiarEstadoAsync(id, partidaDto.EstadoPartida.Value);
                if (!exito)
                {
                    return BadRequest("Transicion de estado no valida");
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PartidasExists(id))
                {
                    return NotFound($"La partida con ID {id} no existe");
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Partidas
        [HttpPost]
        public async Task<ActionResult<PartidasResponseDto>> PostPartidas(PartidasCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validar que el jugador1 existe
            var jugador1 = await _context.Jugadores.FindAsync(dto.Jugador1Id);
            if (jugador1 == null)
            {
                return BadRequest("El Jugador1 no existe");
            }

            // Validar jugador2 solo si se proporciona y no es 0
            if (dto.Jugador2Id.HasValue && dto.Jugador2Id.Value > 0)
            {
                var jugador2 = await _context.Jugadores.FindAsync(dto.Jugador2Id.Value);
                if (jugador2 == null)
                {
                    return BadRequest("El Jugador2 no existe");
                }

                if (dto.Jugador1Id == dto.Jugador2Id)
                {
                    return BadRequest("Un jugador no puede jugar contra sí mismo");
                }
            }

            // Si Jugador2Id es 0, lo establecemos como null
            var jugador2Id = dto.Jugador2Id.HasValue && dto.Jugador2Id.Value > 0 
                ? dto.Jugador2Id.Value 
                : (int?)null;

            var partida = new Partidas
            {
                Jugador1Id = dto.Jugador1Id,
                Jugador2Id = jugador2Id,
                TurnoJugadorId = dto.Jugador1Id,
                EstadoPartida = EstadoPartida.Iniciada,
                EstadoTablero = "000000000",
                FechaInicio = DateTime.UtcNow
            };

            _context.Partidas.Add(partida);
            await _context.SaveChangesAsync();

            // Solo iniciar automáticamente si ya tiene 2 jugadores
            if (jugador2Id.HasValue)
            {
                await _partidasService.IniciarPartidaAsync(partida.PartidaId);
                await _context.Entry(partida).ReloadAsync();
            }

            var response = await GetPartidas(partida.PartidaId);
            return CreatedAtAction("GetPartidas", new { id = partida.PartidaId }, response.Value);
        }

        // POST: api/Partidas/5/unir
        [HttpPost("{partidaId}/unir")]
        public async Task<IActionResult> UnirseAPartida(int partidaId, [FromBody] PartidasJoinDto joinDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (partidaId != joinDto.PartidaId)
            {
                return BadRequest("El ID de la partida no coincide");
            }

            var exito = await _partidasService.UnirJugadorAsync(joinDto.PartidaId, joinDto.JugadorId);
            if (!exito)
            {
                return BadRequest("No se pudo unir el jugador a la partida");
            }

            await _partidasService.IniciarPartidaAsync(partidaId);

            return Ok(new { mensaje = "Jugador unido exitosamente a la partida" });
        }

        // POST: api/Partidas/5/finalizar
        [HttpPost("{id}/finalizar")]
        public async Task<IActionResult> FinalizarPartida(int id, [FromBody] int? ganadorId = null)
        {
            var exito = await _partidasService.FinalizarPartidaAsync(id, ganadorId);
            if (!exito)
            {
                return BadRequest("No se pudo finalizar la partida");
            }

            return Ok(new { mensaje = "Partida finalizada exitosamente" });
        }

        // DELETE: api/Partidas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePartidas(int id)
        {
            var partida = await _context.Partidas.FindAsync(id);
            if (partida == null)
            {
                return NotFound($"No se encontró la partida con ID {id}");
            }

            if (partida.EstadoPartida == EstadoPartida.Finalizada)
            {
                return BadRequest("No se puede eliminar una partida finalizada");
            }

            _context.Partidas.Remove(partida);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Partidas/disponibles
        [HttpGet("disponibles")]
        public async Task<ActionResult<IEnumerable<PartidasListDto>>> GetPartidasDisponibles()
        {
            var partidasDisponibles = await _context.Partidas
                .Include(p => p.Jugador1)
                .Where(p => p.Jugador2Id == null && p.EstadoPartida == EstadoPartida.Iniciada)
                .Select(p => new PartidasListDto
                {
                    PartidaId = p.PartidaId,
                    NombreJugador1 = p.Jugador1 != null ? p.Jugador1.Nombres : "Jugador no encontrado",
                    NombreJugador2 = null,
                    EstadoPartida = p.EstadoPartida,
                    EstadoDescripcion = p.EstadoPartida.GetDescription(),
                    NombreGanador = null,
                    FechaInicio = p.FechaInicio,
                    FechaFin = p.FechaFin,
                    CantidadMovimientos = 0
                })
                .OrderByDescending(p => p.FechaInicio)
                .ToListAsync();

            return Ok(partidasDisponibles);
        }

        private bool PartidasExists(int id)
        {
            return _context.Partidas.Any(e => e.PartidaId == id);
        }
    }
}
