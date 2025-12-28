using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Prestataire
{
    [ApiController]
    [Route("api/prestataire/reviews")]
    [Authorize(Roles = "Prestataire")]
    public class PrestataireReviewsController : ControllerBase
    {
        private readonly PrestataireService _prestataireService;

        public PrestataireReviewsController(PrestataireService prestataireService)
        {
            _prestataireService = prestataireService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyReviews()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var reviews = await _prestataireService.GetReviews(userId);

            var result = reviews.Select(r => new
            {
                id = r.Id,
                rating = r.Rating,
                comment = r.Comment,
                createdAt = r.CreatedAt,
                productName = r.Product?.Name ?? "Produit supprimé",
                customerName = r.User?.FullName ?? "Client inconnu",
                customerEmail = r.User?.Email ?? "Email inconnu"
            });

            return Ok(new { data = result });
        }
    }
}
