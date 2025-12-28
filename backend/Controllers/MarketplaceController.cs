using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/market")]
    public class MarketplaceController : ControllerBase
    {
        private readonly FlowerMarketDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public MarketplaceController(FlowerMarketDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/market/products
        // Retourne tous les produits actifs, avec leur magasin
        [HttpGet("products")]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _context.Products.AsNoTracking()
                .Where(p => p.IsActive)
                .Include(p => p.Store)
                .Include(p => p.Promotions.Where(pr => pr.EndDate > DateTime.UtcNow))
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    category = p.Category,
                    imageUrl = p.ImageUrl,
                    description = p.Description,
                    stock = p.Stock,
                    storeName = p.Store != null ? p.Store.Name : "Boutique Inconnue",
                    // Calcul du prix réduit si promotion active
                    discount = p.Promotions.Any() ? p.Promotions.First().DiscountPercent : 0,
                    finalPrice = p.Promotions.Any() 
                        ? p.Price * (1 - (p.Promotions.First().DiscountPercent / 100.0)) 
                        : p.Price
                })
                .ToListAsync();

            return Ok(new { data = products });
        }

        // GET: api/market/promoted
        // Retourne uniquement les produits en promotion (pour le slider)
        [HttpGet("promoted")]
        public async Task<IActionResult> GetPromotedProducts()
        {
            var promotedProducts = await _context.Products.AsNoTracking()
                .Where(p => p.IsActive && p.Promotions.Any(pr => pr.EndDate > DateTime.UtcNow))
                .Include(p => p.Store)
                .Include(p => p.Promotions)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    originalPrice = p.Price,
                    imageUrl = p.ImageUrl,
                    category = p.Category,
                    promotionTitle = p.Promotions.FirstOrDefault().Title,
                    discount = p.Promotions.FirstOrDefault().DiscountPercent,
                    finalPrice = p.Price * (1 - (p.Promotions.FirstOrDefault().DiscountPercent / 100.0)),
                    endDate = p.Promotions.FirstOrDefault().EndDate
                })
                .ToListAsync();

            return Ok(new { data = promotedProducts });
        }

        // POST: api/market/orders
        // Permet à un client connecté de passer commande
        [HttpPost("orders")]
        [Authorize] // Le client doit être connecté (Token JWT requis)
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            if (dto.Quantity <= 0)
                return BadRequest("La quantité doit être supérieure à 0");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 1. Récupérer le produit
            var product = await _context.Products
                .Include(p => p.Store)
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId);

            if (product == null)
                return NotFound("Produit introuvable");

            if (!product.IsActive)
                return BadRequest("Ce produit n'est plus disponible");

            if (product.Stock < dto.Quantity)
                return BadRequest($"Stock insuffisant. Reste : {product.Stock}");

            // 2. Créer la commande
            var order = new Order
            {
                ProductId = product.Id,
                StoreId = product.StoreId, // Important pour que le Prestataire la voie
                UserId = userId,
                Quantity = dto.Quantity,
                TotalPrice = product.Price * dto.Quantity, // On pourrait appliquer la promo ici aussi si voulu
                CreatedAt = DateTime.UtcNow,
                Status = "pending"
            };

            // 3. Décrémenter le stock
            product.Stock -= dto.Quantity;

            _context.Orders.Add(order);

            // 🔔 NOTIFICATION ADMIN (NOUVELLE COMMANDE)
            var notif = new Notification
            {
                Id = Guid.NewGuid(),
                Title = "Nouvelle Commande",
                Message = $"Commande #{order.Id} reçue pour {product.Store?.Name ?? "Une boutique"}. Montant : {order.TotalPrice} MAD.",
                Type = "Admin",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Notifications.AddAsync(notif);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Commande créée avec succès", orderId = order.Id });
        }


        // POST: api/market/track-visit
        [HttpPost("track-visit")]
        public async Task<IActionResult> TrackVisit([FromQuery] string type = "Client")
        {
            try
            {
                var notif = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = type == "Prestataire" ? "Visite prestataire" : "Visite client",
                    Message = "Un utilisateur a consulté la plateforme",
                    Type = "Admin",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(notif);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch
            {
                return Ok();
            }
        }

        // ✅ MAINTENANT CETTE ROUTE EST BIEN DANS LA CLASSE
        [HttpGet("my-orders")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Product)
                .ThenInclude(p => p.Store)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    productId = o.ProductId,
                    ProductName = o.Product.Name,
                    ProductImage = o.Product.ImageUrl,
                    StoreName = o.Product.Store.Name,
                    o.Quantity,
                    o.TotalPrice,
                    o.Status,
                    o.CreatedAt
                })
                .ToListAsync();

            return Ok(new { data = orders });
        }

        [HttpPost("orders/{id}/pay")]
        [Authorize]
        public async Task<IActionResult> PayOrder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound("Commande introuvable.");

            if (order.Status != "confirmed" && order.Status != "pending")
                return BadRequest("Commande non payable.");

            order.Status = "processing";

            await _context.SaveChangesAsync();

            return Ok(new { message = "Paiement réussi", status = order.Status });
        }

        // DELETE: api/market/orders/{id}
        // Permet au client de supprimer sa commande SI elle est encore en attente/confirmée
        [HttpDelete("orders/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound("Commande introuvable.");

            // On ne peut supprimer que si le statut est "pending" ou "confirmed" (avant paiement/traitement)
            if (order.Status != "pending" && order.Status != "confirmed")
                return BadRequest("Impossible de supprimer une commande en cours de traitement ou déjà payée.");

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Commande supprimée avec succès" });
        }

    }
}
