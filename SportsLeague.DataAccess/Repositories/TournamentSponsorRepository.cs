using Microsoft.EntityFrameworkCore;
using SportsLeague.DataAccess.Context;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Interfaces.Repositories;

namespace SportsLeague.DataAccess.Repositories;

public class TournamentSponsorRepository : GenericRepository<TournamentSponsor>, ITournamentSponsorRepository
{
    public TournamentSponsorRepository(LeagueDbContext context) : base(context)
    {
    }

    public async Task<TournamentSponsor?> GetByTournamentAndSponsorAsync(int tournamentId, int sponsorId) // este método es para evitar que se dupliquen los registros de patrocinadores en un torneo
    {
        return await _dbSet
            .FirstOrDefaultAsync(ts => ts.TournamentId == tournamentId && ts.SponsorId == sponsorId);
    }

    public async Task<IEnumerable<TournamentSponsor>> GetBySponsorAsync(int sponsorId) // este método es para mostrar en la vista de patrocinadores los torneos que patrocinan
    {
        return await _dbSet
            .Where(ts => ts.SponsorId == sponsorId)
            .Include(ts => ts.Tournament)
            .ToListAsync();
    }

    public async Task<IEnumerable<TournamentSponsor>> GetByTournamentAsync(int tournamentId) // este método es para mostrar en la vista de torneos los patrocinadores que tiene cada torneo
    {
        return await _dbSet
            .Where(ts => ts.TournamentId == tournamentId)
            .Include(ts => ts.Sponsor)
            .ToListAsync();
    }
}