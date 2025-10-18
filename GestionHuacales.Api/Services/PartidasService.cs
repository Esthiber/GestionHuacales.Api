using GestionHuacales.Api.DAL;
using GestionHuacales.Api.DTO;
using GestionHuacales.Api.Enums;
using GestionHuacales.Api.Extensions;
using GestionHuacales.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionHuacales.Api.Services;

public class PartidasService(IDbContextFactory<Contexto> DbFactory)
{

    public async Task<bool> CambiarEstadoAsync(int partidaId, EstadoPartida nuevoEstado, string? motivo = null)
    {
        await using var _context = await DbFactory.CreateDbContextAsync();
        var partida = await _context.Partidas.FindAsync(partidaId);
        if (partida == null)
            return false;

        var estadoActual = partida.EstadoPartida;

        if (!estadoActual.EsTransicionValida(nuevoEstado))
            return false;

        switch (nuevoEstado)
        {
            case EstadoPartida.EnProgreso:
                return await IniciarPartidaInternoAsync(partida);

            case EstadoPartida.Finalizada:
                return await FinalizarPartidaInternoAsync(partida);

            default:
                partida.EstadoPartida = nuevoEstado;
                break;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IniciarPartidaAsync(int partidaId)
    {
        await using var _context = await DbFactory.CreateDbContextAsync();

        var partida = await _context.Partidas.FindAsync(partidaId);
        if (partida == null)
            return false;

        if (partida.Jugador2Id == null)
            return false;

        if (partida.EstadoPartida != EstadoPartida.Iniciada)
            return false;

        return await IniciarPartidaInternoAsync(partida);
    }

    private async Task<bool> IniciarPartidaInternoAsync(Partidas partida)
    {
        await using var _context = await DbFactory.CreateDbContextAsync();
        partida.EstadoPartida = EstadoPartida.EnProgreso;

        partida.TurnoJugadorId = partida.Jugador1Id;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> FinalizarPartidaAsync(int partidaId, int? ganadorId = null)
    {
        await using var _context = await DbFactory.CreateDbContextAsync();

        var partida = await _context.Partidas.FindAsync(partidaId);
        if (partida == null)
            return false;

        return await FinalizarPartidaInternoAsync(partida, ganadorId);
    }

    private async Task<bool> FinalizarPartidaInternoAsync(Partidas partida, int? ganadorId = null)
    {
        await using var _context = await DbFactory.CreateDbContextAsync();

        partida.EstadoPartida = EstadoPartida.Finalizada;
        partida.FechaFin = DateTime.UtcNow;
        partida.GanadorId = ganadorId;

        await ActualizarEstadisticasAsync(partida, ganadorId);

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task ActualizarEstadisticasAsync(Partidas partida, int? ganadorId)
    {
        await using var _context = await DbFactory.CreateDbContextAsync();
        var jugador1 = await _context.Jugadores.FindAsync(partida.Jugador1Id);

        if (partida.Jugador2Id == null)
        {
            return;
        }

        var jugador2 = await _context.Jugadores.FindAsync(partida.Jugador2Id);

        if (jugador1 == null || jugador2 == null)
            return;

        if (ganadorId == null)
        {
            jugador1.Empates++;
            jugador2.Empates++;
        }
        else if (ganadorId == partida.Jugador1Id)
        {
            jugador1.Victorias++;
            jugador2.Derrotas++;
        }
        else if (ganadorId == partida.Jugador2Id)
        {
            jugador2.Victorias++;
            jugador1.Derrotas++;
        }
    }

    public async Task<bool> UnirJugadorAsync(int partidaId, int jugadorId)
    {
        await using var _context = await DbFactory.CreateDbContextAsync();

        var partida = await _context.Partidas.FindAsync(partidaId);
        if (partida == null)
            return false;

        if (partida.EstadoPartida != EstadoPartida.Iniciada)
            return false;

        if (partida.Jugador2Id != null)
            return false;

        if (partida.Jugador1Id == jugadorId)
            return false;

        var jugador = await _context.Jugadores.FindAsync(jugadorId);
        if (jugador == null)
            return false;

        partida.Jugador2Id = jugadorId;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<MovimientoResultado> CheckMovimiento(MovimientosCreateDto movimientoDto)
    {
        await using var _context = await DbFactory.CreateDbContextAsync();

        var partida = await _context.Partidas.FindAsync(movimientoDto.PartidaId);
        if (partida == null)
            return new MovimientoResultado { EsValido = false, Mensaje = "Partida no encontrada" };

        if (partida.EstadoPartida != EstadoPartida.EnProgreso)
            return new MovimientoResultado { EsValido = false, Mensaje = "La partida no está en progreso" };

        var posicionOcupada = await _context.Movimientos
            .AnyAsync(m => m.PartidaId == movimientoDto.PartidaId &&
                          m.PosicionFila == movimientoDto.PosicionFila &&
                          m.PosicionColumna == movimientoDto.PosicionColumna);

        if (posicionOcupada)
            return new MovimientoResultado { EsValido = false, Mensaje = "La posición ya está ocupada" };

        var jugadorId = movimientoDto.Jugador == "X" ? partida.Jugador1Id : partida.Jugador2Id ?? 0;

        if (partida.TurnoJugadorId != jugadorId)
            return new MovimientoResultado { EsValido = false, Mensaje = "No es el turno de este jugador" };

        var resultado = await LogicaFin(movimientoDto.PartidaId, movimientoDto);

        return new MovimientoResultado
        {
            EsValido = true,
            TerminaPartida = resultado.HayGanador || resultado.EsEmpate,
            GanadorId = resultado.GanadorId,
            EsEmpate = resultado.EsEmpate,
            Mensaje = resultado.Mensaje
        };
    }

    private async Task<ResultadoFinJuego> LogicaFin(int partidaId, MovimientosCreateDto nuevoMovimiento)
    {
        await using var _context = await DbFactory.CreateDbContextAsync();

        var partida = await _context.Partidas.FindAsync(partidaId);
        if (partida == null)
            return new ResultadoFinJuego { HayGanador = false, EsEmpate = false };

        var movimientos = await _context.Movimientos
            .Where(m => m.PartidaId == partidaId)
            .ToListAsync();

        var tablero = new int[3, 3];

        foreach (var movimiento in movimientos)
        {
            var jugadorMarca = (movimiento.JugadorId == partida.Jugador1Id) ? 1 : 2;
            tablero[movimiento.PosicionFila, movimiento.PosicionColumna] = jugadorMarca;
        }

        var nuevaJugadorMarca = (nuevoMovimiento.Jugador == "X") ? 1 : 2;
        tablero[nuevoMovimiento.PosicionFila, nuevoMovimiento.PosicionColumna] = nuevaJugadorMarca;

        var ganadorMarca = VerificarGanador(tablero);
        
        if (ganadorMarca > 0)
        {
            var ganadorId = (ganadorMarca == 1) ? partida.Jugador1Id : partida.Jugador2Id;
            var nombreGanador = (ganadorMarca == 1) ? "X" : "O";
            
            return new ResultadoFinJuego 
            { 
                HayGanador = true, 
                GanadorId = ganadorId,
                Mensaje = $"¡{nombreGanador} ha ganado!"
            };
        }

        var tableroLleno = EstaTableroLleno(tablero);
        if (tableroLleno)
        {
            return new ResultadoFinJuego 
            { 
                EsEmpate = true,
                Mensaje = "¡Empate! El tablero está lleno."
            };
        }

        // El juego continúa
        return new ResultadoFinJuego 
        { 
            HayGanador = false, 
            EsEmpate = false,
            Mensaje = "El juego continúa"
        };

    }

    /// <summary>
    /// Verifica si hay un ganador en el tablero
    /// </summary>
    /// <param name="tablero">Tablero 3x3</param>
    /// <returns>0 = sin ganador, 1 = ganó X, 2 = ganó O</returns>
    private static int VerificarGanador(int[,] tablero)
    {
        // Verificar filas
        for (int i = 0; i < 3; i++)
        {
            if (tablero[i, 0] != 0 && tablero[i, 0] == tablero[i, 1] && tablero[i, 1] == tablero[i, 2])
                return tablero[i, 0];
        }

        // Verificar columnas
        for (int j = 0; j < 3; j++)
        {
            if (tablero[0, j] != 0 && tablero[0, j] == tablero[1, j] && tablero[1, j] == tablero[2, j])
                return tablero[0, j];
        }

        // Verificar diagonal principal (0,0 -> 1,1 -> 2,2)
        if (tablero[0, 0] != 0 && tablero[0, 0] == tablero[1, 1] && tablero[1, 1] == tablero[2, 2])
            return tablero[0, 0];

        // Verificar diagonal secundaria (0,2 -> 1,1 -> 2,0)
        if (tablero[0, 2] != 0 && tablero[0, 2] == tablero[1, 1] && tablero[1, 1] == tablero[2, 0])
            return tablero[0, 2];

        return 0; // Sin ganador
    }

    /// <summary>
    /// Verifica si el tablero está completamente lleno
    /// </summary>
    /// <param name="tablero">Tablero 3x3</param>
    /// <returns>True si está lleno</returns>
    private static bool EstaTableroLleno(int[,] tablero)
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (tablero[i, j] == 0)
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Actualiza el estado del tablero en la base de datos
    /// </summary>
    /// <param name="partidaId">ID de la partida</param>
    /// <returns>String representando el estado del tablero</returns>
    public async Task<string> ActualizarEstadoTablero(int partidaId)
    {
        await using var _context = await DbFactory.CreateDbContextAsync();

        var partida = await _context.Partidas.FindAsync(partidaId);
        if (partida == null)
            return "000000000";

        var movimientos = await _context.Movimientos
            .Where(m => m.PartidaId == partidaId)
            .ToListAsync();

        var tablero = new int[3, 3];

        foreach (var movimiento in movimientos)
        {
            var jugadorMarca = (movimiento.JugadorId == partida.Jugador1Id) ? 1 : 2;
            tablero[movimiento.PosicionFila, movimiento.PosicionColumna] = jugadorMarca;
        }

        // Convertir tablero a string (000000000 formato)
        var estadoTablero = string.Empty;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                estadoTablero += tablero[i, j].ToString();
            }
        }

        // Actualizar en base de datos
        partida.EstadoTablero = estadoTablero;
        await _context.SaveChangesAsync();

        return estadoTablero;
    }
}

/// <summary>
/// Resultado del análisis de un movimiento
/// </summary>
public class MovimientoResultado
{
    public bool EsValido { get; set; }
    public bool TerminaPartida { get; set; }
    public int? GanadorId { get; set; }
    public bool EsEmpate { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

/// <summary>
/// Resultado del análisis de fin de juego
/// </summary>
public class ResultadoFinJuego
{
    public bool HayGanador { get; set; }
    public bool EsEmpate { get; set; }
    public int? GanadorId { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}

