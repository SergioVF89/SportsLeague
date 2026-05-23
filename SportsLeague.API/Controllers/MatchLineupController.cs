using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SportsLeague.API.DTOs.Request;
using SportsLeague.API.DTOs.Response;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Interfaces.Services;

namespace SportsLeague.API.Controllers;

[ApiController]
[Route("api/match/{matchId}/lineup")]
public class MatchLineupController : ControllerBase  // ← Hereda de ControllerBase, no de BaseController
{
    private readonly IMatchLineupService _matchLineupService;
    private readonly IMapper _mapper;

    public MatchLineupController(IMatchLineupService matchLineupService, IMapper mapper)
    {
        _matchLineupService = matchLineupService;
        _mapper = mapper;
    }

    // POST /api/match/{matchId}/lineup
    [HttpPost]
    public async Task<IActionResult> AddToLineup(int matchId, [FromBody] CreateMatchLineupDTO dto)
    {
        try
        {
            var lineup = _mapper.Map<MatchLineup>(dto);
            var created = await _matchLineupService.AddToLineupAsync(matchId, lineup);

            // Recargar con detalles para el response
            var lineups = await _matchLineupService.GetLineupByMatchAsync(matchId);
            var lineupWithDetails = lineups.FirstOrDefault(l => l.Id == created.Id);

            var responseDto = _mapper.Map<MatchLineupResponseDTO>(lineupWithDetails);

            // Usar CreatedAtAction en lugar de CreatedResponse
            return CreatedAtAction(nameof(GetLineup), new { matchId = matchId }, responseDto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // GET /api/match/{matchId}/lineup
    [HttpGet]
    public async Task<IActionResult> GetLineup(int matchId)
    {
        try
        {
            var lineups = await _matchLineupService.GetLineupByMatchAsync(matchId);
            var responseDtos = _mapper.Map<IEnumerable<MatchLineupResponseDTO>>(lineups);

            // Usar Ok en lugar de OkResponse
            return Ok(new { success = true, message = "Alineación obtenida exitosamente", data = responseDtos });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // GET /api/match/{matchId}/lineup/team/{teamId}
    [HttpGet("team/{teamId}")]
    public async Task<IActionResult> GetLineupByTeam(int matchId, int teamId)
    {
        try
        {
            var lineups = await _matchLineupService.GetLineupByMatchAndTeamAsync(matchId, teamId);
            var responseDtos = _mapper.Map<IEnumerable<MatchLineupResponseDTO>>(lineups);

            return Ok(new { success = true, message = "Alineación del equipo obtenida exitosamente", data = responseDtos });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // DELETE /api/match/{matchId}/lineup/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFromLineup(int matchId, int id)
    {
        try
        {
            await _matchLineupService.DeleteFromLineupAsync(id);

            // Usar NoContent en lugar de NoContentResponse
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}