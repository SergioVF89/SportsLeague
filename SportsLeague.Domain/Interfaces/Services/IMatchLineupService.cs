using SportsLeague.Domain.Entities;

namespace SportsLeague.Domain.Interfaces.Services;

public interface IMatchLineupService
{
    Task<MatchLineup> AddToLineupAsync(int matchId, MatchLineup lineup);
    Task<IEnumerable<MatchLineup>> GetLineupByMatchAsync(int matchId);
    Task<IEnumerable<MatchLineup>> GetLineupByMatchAndTeamAsync(int matchId, int teamId);
    Task DeleteFromLineupAsync(int id);
}
