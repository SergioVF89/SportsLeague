using SportsLeague.Domain.Entities;

namespace SportsLeague.Domain.Interfaces.Repositories
{
    public interface IMatchRepository : IGenericRepository<Match>
    {
        Task<IEnumerable<Match>> GetByTournamentAsync(int tournamentId);
        Task<IEnumerable<Match>> GetByTeamAsync(int teamId);
        Task<Match?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<Match>> GetByTournamentWithDetailsAsync(int tournamentId);
    }

}
