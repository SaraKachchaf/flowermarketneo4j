using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Prestataire
{
    [ApiController]
    [Route("api/prestataire")]
    [Authorize(Roles = "Prestataire")]
    public class PrestataireStatsController : ControllerBase
    {
        private readonly PrestataireService _prestataireService;

        public PrestataireStatsController(PrestataireService prestataireService)
        {
            _prestataireService = prestataireService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var stats = await _prestataireService.GetStats(userId);
            return Ok(new { data = stats });
        }
    }
}
