using Microsoft.Extensions.Logging;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Interfaces.Repositories;
using SportsLeague.Domain.Interfaces.Services;
using System.Text.RegularExpressions;

namespace SportsLeague.Domain.Services;

public partial class SponsorService : ISponsorService
{
    private readonly ISponsorRepository _sponsorRepository;
    private readonly ITournamentRepository _tournamentRepository;
    private readonly ITournamentSponsorRepository _tournamentSponsorRepository;
    private readonly ILogger<SponsorService> _logger;

    public SponsorService(
        ISponsorRepository sponsorRepository,
        ITournamentRepository tournamentRepository,
        ITournamentSponsorRepository tournamentSponsorRepository,
        ILogger<SponsorService> logger)
    {
        _sponsorRepository = sponsorRepository;
        _tournamentRepository = tournamentRepository;
        _tournamentSponsorRepository = tournamentSponsorRepository;
        _logger = logger;
    }

    // ── CRUD Básico ──

    public async Task<IEnumerable<Sponsor>> GetAllAsync() 
    {
        _logger.LogInformation("Retrieving all sponsors");
        return await _sponsorRepository.GetAllAsync();
    }

    public async Task<Sponsor?> GetByIdAsync(int id) // Retorna null si no se encuentra
    {
        _logger.LogInformation("Retrieving sponsor with ID: {SponsorId}", id);
        var sponsor = await _sponsorRepository.GetByIdAsync(id);
        if (sponsor == null)
            _logger.LogWarning("Sponsor with ID {SponsorId} not found", id);
        return sponsor;
    }

    public async Task<Sponsor> CreateAsync(Sponsor sponsor) // Retorna el sponsor creado con su ID asignado
    {
        // Validación: nombre único
        var exists = await _sponsorRepository.ExistsByNameAsync(sponsor.Name);
        if (exists)
        {
            _logger.LogWarning("Sponsor with name '{SponsorName}' already exists", sponsor.Name);
            throw new InvalidOperationException($"Ya existe un patrocinador con el nombre '{sponsor.Name}'");
        }

        // Validación: formato de email
        if (!IsValidEmail(sponsor.ContactEmail))
        {
            _logger.LogWarning("Invalid email format: {Email}", sponsor.ContactEmail);
            throw new InvalidOperationException($"El email '{sponsor.ContactEmail}' no tiene un formato válido");
        }

        _logger.LogInformation("Creating sponsor: {SponsorName}", sponsor.Name);
        return await _sponsorRepository.CreateAsync(sponsor);
    }

    public async Task UpdateAsync(int id, Sponsor sponsor) // No retorna nada, pero lanza excepción si no se encuentra o si hay validaciones fallidas
    {
        var existingSponsor = await _sponsorRepository.GetByIdAsync(id); 
        if (existingSponsor == null)
        {
            throw new KeyNotFoundException($"No se encontró el patrocinador con ID {id}");
        }

        // Validar nombre único (si cambió)
        if (existingSponsor.Name != sponsor.Name)
        {
            var exists = await _sponsorRepository.ExistsByNameAsync(sponsor.Name, id);
            if (exists)
            {
                throw new InvalidOperationException($"Ya existe un patrocinador con el nombre '{sponsor.Name}'");
            }
        }

        // Validación: formato de email
        if (!IsValidEmail(sponsor.ContactEmail))
        {
            throw new InvalidOperationException($"El email '{sponsor.ContactEmail}' no tiene un formato válido");
        }

        existingSponsor.Name = sponsor.Name;
        existingSponsor.ContactEmail = sponsor.ContactEmail;
        existingSponsor.Phone = sponsor.Phone;
        existingSponsor.WebsiteUrl = sponsor.WebsiteUrl;
        existingSponsor.Category = sponsor.Category;

        _logger.LogInformation("Updating sponsor with ID: {SponsorId}", id);
        await _sponsorRepository.UpdateAsync(existingSponsor);
    }

    public async Task DeleteAsync(int id) 
    {
        var exists = await _sponsorRepository.ExistsAsync(id);
        if (!exists)
        {
            throw new KeyNotFoundException($"No se encontró el patrocinador con ID {id}");
        }

        _logger.LogInformation("Deleting sponsor with ID: {SponsorId}", id);
        await _sponsorRepository.DeleteAsync(id);
    }

    // ── Métodos de Vinculación ──

    public async Task<TournamentSponsor> LinkToTournamentAsync(int sponsorId, int tournamentId, decimal contractAmount) // Retorna la entidad de vinculación creada, con su ID asignado
    {
        // Validar que el sponsor existe
        var sponsor = await _sponsorRepository.GetByIdAsync(sponsorId);
        if (sponsor == null)
        {
            throw new KeyNotFoundException($"No se encontró el patrocinador con ID {sponsorId}");
        }

        // Validar que el torneo existe
        var tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament == null)
        {
            throw new KeyNotFoundException($"No se encontró el torneo con ID {tournamentId}");
        }

        // Validar que ContractAmount sea mayor a 0
        if (contractAmount <= 0)
        {
            throw new InvalidOperationException("El monto del contrato debe ser mayor a 0");
        }

        // Validar que no exista ya la vinculación
        var existing = await _tournamentSponsorRepository
            .GetByTournamentAndSponsorAsync(tournamentId, sponsorId);
        if (existing != null)
        {
            throw new InvalidOperationException("Este patrocinador ya está vinculado a este torneo");
        }

        var tournamentSponsor = new TournamentSponsor
        {
            TournamentId = tournamentId,
            SponsorId = sponsorId,
            ContractAmount = contractAmount,
            JoinedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Linking sponsor {SponsorId} to tournament {TournamentId} with amount {Amount}",
            sponsorId, tournamentId, contractAmount);

        return await _tournamentSponsorRepository.CreateAsync(tournamentSponsor);
    }

    public async Task UnlinkFromTournamentAsync(int sponsorId, int tournamentId) 
    {
        var link = await _tournamentSponsorRepository
            .GetByTournamentAndSponsorAsync(tournamentId, sponsorId);

        if (link == null)
        {
            throw new KeyNotFoundException($"No se encontró la vinculación entre el sponsor {sponsorId} y el torneo {tournamentId}");
        }

        _logger.LogInformation("Unlinking sponsor {SponsorId} from tournament {TournamentId}", sponsorId, tournamentId);
        await _tournamentSponsorRepository.DeleteAsync(link.Id);
    }

    public async Task<IEnumerable<Tournament>> GetTournamentsBySponsorAsync(int sponsorId) // Retorna la lista de torneos a los que el sponsor está vinculado
    {
        var sponsorExists = await _sponsorRepository.ExistsAsync(sponsorId);
        if (!sponsorExists)
        {
            throw new KeyNotFoundException($"No se encontró el patrocinador con ID {sponsorId}");
        }

        var tournamentSponsors = await _tournamentSponsorRepository.GetBySponsorAsync(sponsorId);
        return tournamentSponsors.Select(ts => ts.Tournament);
    }

    // ── Métodos Privados de Ayuda ──

    private static bool IsValidEmail(string email) // Método privado para validar el formato de email utilizando una expresión regular simple
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // Expresión regular para validar email
            return EmailRegex().IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex(); // Este método utiliza el atributo [GeneratedRegex] para generar una expresión regular compilada que valida un formato básico de email. Es más eficiente que crear una nueva instancia de Regex cada vez.
}