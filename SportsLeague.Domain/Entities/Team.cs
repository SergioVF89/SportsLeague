namespace SportsLeague.Domain.Entities;

public class Team : AuditBase
{
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Stadium { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public DateTime FoundedDate { get; set; }

    // Navigation Property - Colección de jugadores

    public ICollection<Player> Players { get; set; } = new List<Player>(); // Acceso: team.Players.Count
}
