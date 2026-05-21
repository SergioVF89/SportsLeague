using Microsoft.Extensions.Logging;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Enums;
using SportsLeague.Domain.Interfaces.Repositories;

namespace SportsLeague.Domain.Services;

public class StandingsService : IStandingsService
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly ITournamentTeamRepository _tournamentTeamRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IMatchResultRepository _matchResultRepository;
    private readonly IGoalRepository _goalRepository;
    private readonly ICardRepository _cardRepository;
    private readonly ILogger<StandingsService> _logger;

    public StandingsService(
        ITournamentRepository tournamentRepository,
        ITournamentTeamRepository tournamentTeamRepository,
        IMatchRepository matchRepository,
        IMatchResultRepository matchResultRepository,
        IGoalRepository goalRepository,
        ICardRepository cardRepository,
        ILogger<StandingsService> logger)
    {
        _tournamentRepository = tournamentRepository;
        _tournamentTeamRepository = tournamentTeamRepository;
        _matchRepository = matchRepository;
        _matchResultRepository = matchResultRepository;
        _goalRepository = goalRepository;
        _cardRepository = cardRepository;
        _logger = logger;
    }

    public async Task<object> GetStandingsAsync(int tournamentId)
    {
        var tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament == null)
            throw new KeyNotFoundException(
                $"No se encontró el torneo con ID {tournamentId}");

        // Obtener equipos inscritos
        var tournamentTeams = await _tournamentTeamRepository
            .GetByTournamentAsync(tournamentId);

        // Obtener partidos finalizados del torneo
        var matches = (await _matchRepository
            .GetByTournamentAsync(tournamentId))
            .Where(m => m.Status == MatchStatus.Finished)
            .ToList();

        // Obtener resultados de esos partidos
        var matchIds = matches.Select(m => m.Id).ToHashSet();
        var allResults = new List<MatchResult>();
        foreach (var matchId in matchIds)
        {
            var result = await _matchResultRepository.GetByMatchIdAsync(matchId);
            if (result != null) allResults.Add(result);
        }

        // Construir diccionario matchId -> result para acceso rápido
        var resultsByMatch = allResults.ToDictionary(r => r.MatchId);

        // Calcular standings para cada equipo
        var standings = tournamentTeams.Select(tt =>
        {
            var teamId = tt.TeamId;
            var teamName = tt.Team.Name;

            // Partidos como local con resultado
            var homeMatches = matches
                .Where(m => m.HomeTeamId == teamId && resultsByMatch.ContainsKey(m.Id))
                .Select(m => resultsByMatch[m.Id])
                .ToList();

            // Partidos como visitante con resultado
            var awayMatches = matches
                .Where(m => m.AwayTeamId == teamId && resultsByMatch.ContainsKey(m.Id))
                .Select(m => resultsByMatch[m.Id])
                .ToList();

            // Calcular estadísticas
            int homeWins = homeMatches.Count(r => r.HomeGoals > r.AwayGoals);
            int homeDraws = homeMatches.Count(r => r.HomeGoals == r.AwayGoals);
            int homeLosses = homeMatches.Count(r => r.HomeGoals < r.AwayGoals);
            int homeGF = homeMatches.Sum(r => r.HomeGoals);
            int homeGC = homeMatches.Sum(r => r.AwayGoals);

            int awayWins = awayMatches.Count(r => r.AwayGoals > r.HomeGoals);
            int awayDraws = awayMatches.Count(r => r.AwayGoals == r.HomeGoals);
            int awayLosses = awayMatches.Count(r => r.AwayGoals < r.HomeGoals);
            int awayGF = awayMatches.Sum(r => r.AwayGoals);
            int awayGC = awayMatches.Sum(r => r.HomeGoals);

            int totalWins = homeWins + awayWins;
            int totalDraws = homeDraws + awayDraws;
            int totalLosses = homeLosses + awayLosses;
            int totalGF = homeGF + awayGF;
            int totalGC = homeGC + awayGC;

            return new
            {
                TeamId = teamId,
                TeamName = teamName,
                MatchesPlayed = homeMatches.Count + awayMatches.Count,
                Wins = totalWins,
                Draws = totalDraws,
                Losses = totalLosses,
                GoalsFor = totalGF,
                GoalsAgainst = totalGC,
                GoalDifference = totalGF - totalGC,
                Points = (totalWins * 3) + totalDraws
            };
        })
        .OrderByDescending(s => s.Points)
        .ThenByDescending(s => s.GoalDifference)
        .ThenByDescending(s => s.GoalsFor)
        .Select((s, index) => new
        {
            Position = index + 1,
            s.TeamId,
            s.TeamName,
            s.MatchesPlayed,
            s.Wins,
            s.Draws,
            s.Losses,
            s.GoalsFor,
            s.GoalsAgainst,
            s.GoalDifference,
            s.Points
        })
        .ToList();

        _logger.LogInformation(
            "Generated standings for tournament {TournamentId}", tournamentId);
        return standings;
    }
}



