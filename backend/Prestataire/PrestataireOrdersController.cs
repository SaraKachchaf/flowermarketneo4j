using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Prestataire
{
    [ApiController]
    [Route("api/prestataire/orders")]
    [Authorize(Roles = "Prestataire")]
    public class PrestataireOrdersController : ControllerBase
    {
        private readonly PrestataireService _prestataireService;

        public PrestataireOrdersController(PrestataireService prestataireService)
        {
            _prestataireService = prestataireService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var orders = await _prestataireService.GetOrders(userId);

            var result = orders.Select(o => new
            {
                id = o.Id,
                createdAt = o.CreatedAt,
                status = o.Status,
                totalAmount = o.TotalPrice,
                customerName = o.User?.FullName ?? "Client inconnu",
                customerEmail = o.User?.Email ?? "Email inconnu",
                productName = o.Product?.Name ?? "Produit supprimé",
                quantity = o.Quantity
            });

            return Ok(new { data = result });
        }

        public class UpdateOrderStatusRequest
        {
            [System.Text.Json.Serialization.JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Status)) 
                return BadRequest(new { error = "status is required" });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var success = await _prestataireService.UpdateOrderStatus(userId, id, request.Status);

            if (!success) return NotFound(new { error = "Order not found or not owned by you." });

            return Ok(new { success = true });
        }
    }
}
