using Microsoft.EntityFrameworkCore;
using SportsLeague.DataAccess.Context;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Interfaces.Repositories;

namespace SportsLeague.DataAccess.Repositories;

public class SponsorRepository : GenericRepository<Sponsor>, ISponsorRepository
{
    public SponsorRepository(LeagueDbContext context) : base(context) // el constructor simplemente llama al constructor de la clase base para inicializar el contexto y el DbSet
    {
    }

    public async Task<Sponsor?> GetByNameAsync(string name) // este método es útil para obtener un sponsor por su nombre, por ejemplo, para mostrar detalles o validar la existencia
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)// este método es útil para validar la unicidad del nombre al crear o actualizar un sponsor
    {
        var query = _dbSet.Where(s => s.Name.ToLower() == name.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}
