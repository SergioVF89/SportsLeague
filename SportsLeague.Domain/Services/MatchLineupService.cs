using Microsoft.Extensions.Logging;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Enums;
using SportsLeague.Domain.Helpers;
using SportsLeague.Domain.Interfaces.Repositories;
using SportsLeague.Domain.Interfaces.Services;

namespace SportsLeague.Domain.Services;

public class MatchLineupService : IMatchLineupService
{
    private readonly IMatchLineupRepository _matchLineupRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly MatchValidationHelper _validationHelper;
    private readonly ILogger<MatchLineupService> _logger;

    public MatchLineupService(
        IMatchLineupRepository matchLineupRepository,
        IMatchRepository matchRepository,
        MatchValidationHelper validationHelper,
        ILogger<MatchLineupService> logger)
    {
        _matchLineupRepository = matchLineupRepository;
        _matchRepository = matchRepository;
        _validationHelper = validationHelper;
        _logger = logger;
    }

    public async Task<MatchLineup> AddToLineupAsync(int matchId, MatchLineup lineup)
    {
        // V1: Validar que el partido existe
        var match = await _validationHelper.ValidateMatchForEventAsync(matchId);

        // V6: Validar que el partido está en estado Scheduled
        if (match.Status != MatchStatus.Scheduled)
        {
            throw new InvalidOperationException(
                "Solo se pueden registrar alineaciones en partidos con estado Scheduled");
        }

        // V2: Validar que el jugador existe y V3: pertenece al HomeTeam o AwayTeam
        var player = await _validationHelper.ValidatePlayerInMatchAsync(lineup.PlayerId, match);

        // V4: Validar que el jugador no está ya registrado en la alineación
        var existing = await _matchLineupRepository
            .GetByMatchAndPlayerAsync(matchId, lineup.PlayerId);

        if (existing != null)
        {
            throw new InvalidOperationException(
                "El jugador ya está registrado en la alineación de este partido");
        }

        // V5: Si es titular, validar máximo 11 titulares por equipo
        if (lineup.IsStarter)
        {
            var startersCount = await _matchLineupRepository
                .CountStartersByMatchAndTeamAsync(matchId, player.TeamId);

            if (startersCount >= 11)
            {
                throw new InvalidOperationException(
                    "El equipo ya tiene 11 titulares registrados en este partido");
            }
        }

        lineup.MatchId = matchId;
        lineup.PlayerId = lineup.PlayerId;

        _logger.LogInformation(
            "Added player {PlayerId} to lineup for match {MatchId} as {StarterStatus}",
            lineup.PlayerId, matchId, lineup.IsStarter ? "Starter" : "Substitute");

        return await _matchLineupRepository.CreateAsync(lineup);
    }

    public async Task<IEnumerable<MatchLineup>> GetLineupByMatchAsync(int matchId)
    {
        var match = await _matchRepository.GetByIdAsync(matchId);
        if (match == null)
        {
            throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");
        }

        return await _matchLineupRepository.GetByMatchAsync(matchId);
    }

    public async Task<IEnumerable<MatchLineup>> GetLineupByMatchAndTeamAsync(int matchId, int teamId)
    {
        var match = await _matchRepository.GetByIdAsync(matchId);
        if (match == null)
        {
            throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");
        }

        // Validar que el equipo existe (opcional)
        if (match.HomeTeamId != teamId && match.AwayTeamId != teamId)
        {
            throw new InvalidOperationException(
                $"El equipo con ID {teamId} no participa en este partido");
        }

        return await _matchLineupRepository.GetByMatchAndTeamAsync(matchId, teamId);
    }

    public async Task DeleteFromLineupAsync(int lineupId)
    {
        var exists = await _matchLineupRepository.ExistsAsync(lineupId);
        if (!exists)
        {
            throw new KeyNotFoundException($"No se encontró la alineación con ID {lineupId}");
        }

        _logger.LogInformation("Deleted lineup entry with ID {LineupId}", lineupId);
        await _matchLineupRepository.DeleteAsync(lineupId);
    }
}

