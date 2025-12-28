using backend.Models;
using backend.Prestataire.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Prestataire
{
    [ApiController]
    [Route("api/prestataire/promotions")]
    [Authorize(Roles = "Prestataire")]
    public class PrestatairePromotionsController : ControllerBase
    {
        private readonly PrestataireService _prestataireService;

        public PrestatairePromotionsController(PrestataireService prestataireService)
        {
            _prestataireService = prestataireService;
        }

        private string? GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyPromotions()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var promotions = await _prestataireService.GetMyPromotions(userId);

            var result = promotions.Select(p => new
            {
                id = p.Id,
                title = p.Title,
                code = p.Code,
                discount = p.DiscountPercent,
                startDate = p.StartDate,
                endDate = p.EndDate,
                usageCount = p.UsageCount,
                usageLimit = p.UsageLimit,
                productId = p.ProductId,
                productName = p.Product?.Name ?? "Produit supprimé"
            });

            return Ok(new { data = result });
        }

        [HttpPost]
        public async Task<IActionResult> CreatePromotion([FromBody] Promotion dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var createDto = new CreatePromotionDto
            {
                ProductId = dto.ProductId,
                Title = dto.Title ?? "Promotion",
                Description = dto.Description ?? "",
                DiscountPercent = dto.DiscountPercent,
                StartDate = dto.StartDate == default ? DateTime.UtcNow : dto.StartDate,
                EndDate = dto.EndDate == default ? DateTime.UtcNow.AddDays(7) : dto.EndDate,
                Code = dto.Code,
                UsageLimit = dto.UsageLimit
            };

            await _prestataireService.AddPromotion(userId, createDto);

            return Ok(new { success = true });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePromotion(int id, [FromBody] Promotion dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var success = await _prestataireService.UpdatePromotion(userId, id, dto);

            if (!success) return NotFound(new { error = "Promotion not found." });

            return Ok(new { success = true });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var success = await _prestataireService.DeletePromotion(userId, id);

            if (!success) return NotFound(new { error = "Promotion not found." });

            return Ok(new { message = "Deleted" });
        }
    }
}
