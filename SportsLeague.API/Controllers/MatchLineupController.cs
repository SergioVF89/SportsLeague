using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SportsLeague.API.DTOs.Request;
using SportsLeague.API.DTOs.Response;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Interfaces.Services;

namespace SportsLeague.API.Controllers;

[ApiController]
[Route("api/match/{matchId}/lineup")]
public class MatchLineupController : ControllerBase
{
    private readonly IMatchLineupService _matchLineupService;
    private readonly IMapper _mapper;

    public MatchLineupController(
        IMatchLineupService matchLineupService,
        IMapper mapper)
    {
        _matchLineupService = matchLineupService;
        _mapper = mapper;
    }

    // POST /api/match/{matchId}/lineup
    [HttpPost]
    public async Task<ActionResult<MatchLineupResponseDTO>> AddToLineup(
        int matchId,
        CreateMatchLineupDTO dto)
    {
        try
        {
            var lineup = _mapper.Map<MatchLineup>(dto);
            var created = await _matchLineupService.AddToLineupAsync(matchId, lineup);

            // Recargar para obtener las Navigation Properties (Player, Team)
            var lineups = await _matchLineupService.GetLineupByMatchAsync(matchId);
            var createdLineup = lineups.FirstOrDefault(l => l.Id == created.Id);

            return CreatedAtAction(
                nameof(GetLineup),
                new { matchId = matchId },
                _mapper.Map<MatchLineupResponseDTO>(createdLineup));
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
    public async Task<ActionResult<IEnumerable<MatchLineupResponseDTO>>> GetLineup(int matchId)
    {
        try
        {
            var lineups = await _matchLineupService.GetLineupByMatchAsync(matchId);
            return Ok(_mapper.Map<IEnumerable<MatchLineupResponseDTO>>(lineups));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // GET /api/match/{matchId}/lineup/team/{teamId}
    [HttpGet("team/{teamId}")]
    public async Task<ActionResult<IEnumerable<MatchLineupResponseDTO>>> GetLineupByTeam(
        int matchId,
        int teamId)
    {
        try
        {
            var lineups = await _matchLineupService.GetLineupByMatchAndTeamAsync(matchId, teamId);
            return Ok(_mapper.Map<IEnumerable<MatchLineupResponseDTO>>(lineups));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE /api/match/{matchId}/lineup/{lineupId}
    [HttpDelete("{lineupId}")]
    public async Task<ActionResult> DeleteFromLineup(int matchId, int lineupId)
    {
        try
        {
            await _matchLineupService.DeleteFromLineupAsync(lineupId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
