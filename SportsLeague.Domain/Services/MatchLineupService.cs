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
    private readonly IPlayerRepository _playerRepository;
    private readonly MatchValidationHelper _validationHelper;
    private readonly ILogger<MatchLineupService> _logger;

    public MatchLineupService(
        IMatchLineupRepository matchLineupRepository,
        IMatchRepository matchRepository,
        IPlayerRepository playerRepository,
        MatchValidationHelper validationHelper,
        ILogger<MatchLineupService> logger)
    {
        _matchLineupRepository = matchLineupRepository;
        _matchRepository = matchRepository;
        _playerRepository = playerRepository;
        _validationHelper = validationHelper;
        _logger = logger;
    }

    public async Task<MatchLineup> AddToLineupAsync(int matchId, MatchLineup lineup)
    {
        // V1: Validar que el partido existe
        var match = await _matchRepository.GetByIdAsync(matchId);
        if (match == null)
        {
            _logger.LogWarning("Match with ID {MatchId} not found", matchId);
            throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");
        }

        // V2: Validar que el jugador existe
        var player = await _playerRepository.GetByIdAsync(lineup.PlayerId);
        if (player == null)
        {
            _logger.LogWarning("Player with ID {PlayerId} not found", lineup.PlayerId);
            throw new KeyNotFoundException($"No se encontró el jugador con ID {lineup.PlayerId}");
        }

        // V3: Validar que el jugador pertenece al HomeTeam o AwayTeam del partido
        if (player.TeamId != match.HomeTeamId && player.TeamId != match.AwayTeamId)
        {
            _logger.LogWarning("Player {PlayerId} does not belong to either team of match {MatchId}",
                lineup.PlayerId, matchId);
            throw new InvalidOperationException(
                "El jugador no pertenece a ninguno de los equipos del partido");
        }

        // V4: Validar que el jugador no esté ya registrado en la alineación
        var exists = await _matchLineupRepository.ExistsByMatchAndPlayerAsync(matchId, lineup.PlayerId);
        if (exists)
        {
            _logger.LogWarning("Player {PlayerId} already in lineup for match {MatchId}",
                lineup.PlayerId, matchId);
            throw new InvalidOperationException(
                "El jugador ya está registrado en la alineación de este partido");
        }

        // V5: Si es titular, validar máximo 11 titulares por equipo
        if (lineup.IsStarter)
        {
            var startersCount = await _matchLineupRepository.CountStartersByMatchAndTeamAsync(
                matchId, player.TeamId);

            if (startersCount >= 11)
            {
                _logger.LogWarning("Team {TeamId} already has 11 starters for match {MatchId}",
                    player.TeamId, matchId);
                throw new InvalidOperationException(
                    "El equipo ya tiene 11 titulares registrados en este partido");
            }
        }

        // V6: Validar que el partido está en estado Scheduled
        if (match.Status != MatchStatus.Scheduled)
        {
            _logger.LogWarning("Match {MatchId} is not in Scheduled status (current: {Status})",
                matchId, match.Status);
            throw new InvalidOperationException(
                "Solo se pueden registrar alineaciones en partidos con estado Scheduled");
        }

        lineup.MatchId = matchId;

        _logger.LogInformation(
            "Adding player {PlayerId} to lineup for match {MatchId} as {StarterStatus}",
            lineup.PlayerId, matchId, lineup.IsStarter ? "starter" : "substitute");

        return await _matchLineupRepository.CreateAsync(lineup);
    }

    public async Task<IEnumerable<MatchLineup>> GetLineupByMatchAsync(int matchId)
    {
        // Validar que el partido existe
        var match = await _matchRepository.GetByIdAsync(matchId);
        if (match == null)
        {
            _logger.LogWarning("Match with ID {MatchId} not found", matchId);
            throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");
        }

        _logger.LogInformation("Retrieving full lineup for match {MatchId}", matchId);
        return await _matchLineupRepository.GetByMatchWithDetailsAsync(matchId);
    }

    public async Task<IEnumerable<MatchLineup>> GetLineupByMatchAndTeamAsync(int matchId, int teamId)
    {
        // Validar que el partido existe
        var match = await _matchRepository.GetByIdAsync(matchId);
        if (match == null)
        {
            _logger.LogWarning("Match with ID {MatchId} not found", matchId);
            throw new KeyNotFoundException($"No se encontró el partido con ID {matchId}");
        }

        // Validar que el equipo es local o visitante del partido
        if (teamId != match.HomeTeamId && teamId != match.AwayTeamId)
        {
            _logger.LogWarning("Team {TeamId} does not participate in match {MatchId}", teamId, matchId);
            throw new InvalidOperationException(
                "El equipo no participa en este partido");
        }

        _logger.LogInformation("Retrieving lineup for match {MatchId} and team {TeamId}", matchId, teamId);
        return await _matchLineupRepository.GetByMatchAndTeamAsync(matchId, teamId);
    }

    public async Task DeleteFromLineupAsync(int id)
    {
        var exists = await _matchLineupRepository.ExistsAsync(id);
        if (!exists)
        {
            _logger.LogWarning("MatchLineup with ID {Id} not found for deletion", id);
            throw new KeyNotFoundException($"No se encontró la alineación con ID {id}");
        }

        _logger.LogInformation("Deleting match lineup with ID: {Id}", id);
        await _matchLineupRepository.DeleteAsync(id);
    }
}

