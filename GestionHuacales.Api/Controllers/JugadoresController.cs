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

namespace GestionHuacales.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JugadoresController : ControllerBase
    {
        private readonly Contexto _context;

        public JugadoresController(Contexto context)
        {
            _context = context;
        }

        // GET: api/Jugadores
        [HttpGet]
        public async Task<ActionResult<IEnumerable<JugadoresListDto>>> GetJugadores()
        {
            var jugadores = await _context.Jugadores
                .Select(j => new JugadoresListDto
                {
                    JugadorId = j.JugadorId,
                    Nombres = j.Nombres,
                    Email = j.Email,
                    Victorias = j.Victorias,
                    Derrotas = j.Derrotas,
                    Empates = j.Empates
                })
                .ToListAsync();

            return Ok(jugadores);
        }

        // GET: api/Jugadores/5
        [HttpGet("{id}")]
        public async Task<ActionResult<JugadoresResponseDto>> GetJugadores(int id)
        {
            var jugador = await _context.Jugadores
                .Where(j => j.JugadorId == id)
                .Select(j => new JugadoresResponseDto
                {
                    JugadorId = j.JugadorId,
                    Nombres = j.Nombres,
                    Email = j.Email,
                    FechaCreacion = j.FechaCreacion,
                    Victorias = j.Victorias,
                    Derrotas = j.Derrotas,
                    Empates = j.Empates
                })
                .FirstOrDefaultAsync();

            if (jugador == null)
            {
                return NotFound($"No se encontró el jugador con ID {id}");
            }

            return Ok(jugador);
        }

        // GET: api/Jugadores/5/stats
        [HttpGet("{id}/stats")]
        public async Task<ActionResult<JugadoresStatsDto>> GetJugadorStats(int id)
        {
            var jugador = await _context.Jugadores
                .Where(j => j.JugadorId == id)
                .Select(j => new JugadoresStatsDto
                {
                    JugadorId = j.JugadorId,
                    Nombres = j.Nombres,
                    Victorias = j.Victorias,
                    Derrotas = j.Derrotas,
                    Empates = j.Empates,
                    FechaCreacion = j.FechaCreacion,
                    FechaUltimaPartida = j.PartidasComoJugador1
                        .Union(j.PartidasComoJugador2)
                        .Where(p => p.FechaFin != null)
                        .OrderByDescending(p => p.FechaFin)
                        .Select(p => p.FechaFin)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (jugador == null)
            {
                return NotFound($"No se encontró el jugador con ID {id}");
            }

            return Ok(jugador);
        }

        // PUT: api/Jugadores/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutJugadores(int id, JugadoresUpdateDto jugadorDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var jugadorExistente = await _context.Jugadores.FindAsync(id);
            if (jugadorExistente == null)
            {
                return NotFound($"No se encontro el jugador con ID {id}");
            }

            var emailExiste = await _context.Jugadores
                .AnyAsync(j => j.Email == jugadorDto.Email && j.JugadorId != id);

            var nombreExiste = await _context.Jugadores
                .AnyAsync(j => j.Nombres == jugadorDto.Nombres && j.JugadorId != id);

            if (emailExiste)
                return BadRequest("El email ya esta siendo utilizado por otro jugador");
            if (nombreExiste)
                return BadRequest("El nombre ya esta siendo utilizado por otro jugador");

            jugadorExistente.Nombres = jugadorDto.Nombres;
            jugadorExistente.Email = jugadorDto.Email;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!JugadoresExists(id))
                {
                    return NotFound($"El jugador con ID {id} no existe");
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Jugadores
        [HttpPost]
        public async Task<ActionResult<JugadoresResponseDto>> PostJugadores(JugadoresCreateDto jugadorDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var emailExiste = await _context.Jugadores
                .AnyAsync(j => j.Email == jugadorDto.Email);

            var nombreExiste = await _context.Jugadores
                .AnyAsync(j => j.Nombres == jugadorDto.Nombres);

            if (emailExiste)
            {
                return BadRequest("El email ya esta siendo utilizado por otro jugador");
            }
            if (nombreExiste)
            {
                return BadRequest("El nombre ya esta siendo utilizado por otro jugador");
            }

            var jugador = new Jugadores
            {
                Nombres = jugadorDto.Nombres,
                Email = jugadorDto.Email,
                FechaCreacion = DateTime.UtcNow,
                Victorias = 0,
                Derrotas = 0,
                Empates = 0
            };

            _context.Jugadores.Add(jugador);
            await _context.SaveChangesAsync();

            var response = new JugadoresResponseDto
            {
                JugadorId = jugador.JugadorId,
                Nombres = jugador.Nombres,
                Email = jugador.Email,
                FechaCreacion = jugador.FechaCreacion,
                Victorias = jugador.Victorias,
                Derrotas = jugador.Derrotas,
                Empates = jugador.Empates
            };

            return CreatedAtAction("GetJugadores", new { id = jugador.JugadorId }, response);
        }

        // DELETE: api/Jugadores/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJugadores(int id)
        {
            var jugador = await _context.Jugadores.FindAsync(id);
            if (jugador == null)
            {
                return NotFound($"No se encontró el jugador con ID {id}");
            }

            var tienePartidasActivas = await _context.Partidas
                .AnyAsync(p => (p.Jugador1Id == id || p.Jugador2Id == id) && p.EstadoPartida != EstadoPartida.Finalizada);

            if (tienePartidasActivas)
            {
                return BadRequest("No se puede eliminar el jugador porque tiene partidas activas");
            }

            _context.Jugadores.Remove(jugador);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Jugadores/ranking
        [HttpGet("ranking")]
        public async Task<ActionResult<IEnumerable<JugadoresListDto>>> GetRanking()
        {
            var ranking = await _context.Jugadores
                .Where(j => j.Victorias + j.Derrotas + j.Empates > 0)
                .OrderByDescending(j => (double)j.Victorias / (j.Victorias + j.Derrotas + j.Empates) * 100)
                .ThenByDescending(j => j.Victorias)
                .ThenBy(j => j.Nombres)
                .Select(j => new JugadoresListDto
                {
                    JugadorId = j.JugadorId,
                    Nombres = j.Nombres,
                    Email = j.Email,
                    Victorias = j.Victorias,
                    Derrotas = j.Derrotas,
                    Empates = j.Empates
                })
                .ToListAsync();

            return Ok(ranking);
        }

        private bool JugadoresExists(int id)
        {
            return _context.Jugadores.Any(e => e.JugadorId == id);
        }
    }
}
