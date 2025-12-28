using backend.Models;
using backend.Prestataire.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Prestataire
{
    [ApiController]
    [Authorize(Roles = "Prestataire")]
    [Route("api/prestataire/products")]
    public class PrestataireProductsController : ControllerBase
    {
        private readonly PrestataireService _prestataireService;

        public PrestataireProductsController(PrestataireService prestataireService)
        {
            _prestataireService = prestataireService;
        }

        private string? GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // POST /api/prestataire/products
        [HttpPost]
        public async Task<IActionResult> CreateProduct(
          [FromForm] string name,
          [FromForm] double price,
          [FromForm] int stock,
          [FromForm] string category,
          [FromForm] string description,
          [FromForm] string? imageUrl,
          [FromForm] IFormFile? image
        )
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            string? finalImagePath = imageUrl;

            if (image != null && image.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                var fullPath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
                finalImagePath = "/uploads/" + fileName;
            }

            var dto = new CreateProductDto
            {
                Name = name,
                Price = price,
                Stock = stock,
                Category = category,
                Description = description,
                ImageUrl = finalImagePath
            };

            var product = await _prestataireService.AddProduct(userId, dto);
            if (product == null) return BadRequest("Store introuvable");

            return Ok(new { data = product });
        }

        // PUT /api/prestataire/products/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var success = await _prestataireService.UpdateProduct(id, userId, dto);
            if (!success) return NotFound("Produit introuvable ou vous n'avez pas les droits.");

            return Ok(new { data = dto });
        }

        // DELETE /api/prestataire/products/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Verification layer could be added in service to ensure user owns the product
            await _prestataireService.DeleteProduct(id);
            return Ok(new { message = "Produit supprimé" });
        }

        [HttpGet]
        public async Task<IActionResult> GetMyProducts()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var products = await _prestataireService.GetProducts(userId);
            return Ok(new { data = products });
        }
    }
}
