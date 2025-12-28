using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;
using System.Security.Claims;

namespace backend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly FlowerMarketDbContext _context;

        public CartController(FlowerMarketDbContext context)
        {
            _context = context;
        }

        // GET: api/cart
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                    .ThenInclude(p => p.Store)
                        .ThenInclude(s => s.Prestataire)
                .Select(c => new
                {
                    c.Id,
                    c.ProductId,
                    ProductName = c.Product.Name,
                    ProductPrice = c.Product.Price,
                    ProductImage = c.Product.ImageUrl,
                    c.Quantity,
                    TotalPrice = c.Product.Price * c.Quantity,
                    StoreName = c.Product.Store.Name,
                    StoreId = c.Product.StoreId,
                    StoreAddress = c.Product.Store.Address,
                    StoreCity = c.Product.Store.Address, // Fallback to Address as City is missing
                    StoreEmail = c.Product.Store.Prestataire.Email,
                    c.AddedAt
                })
                .ToListAsync();

            var total = cartItems.Sum(item => item.TotalPrice);

            return Ok(new
            {
                success = true,
                data = new
                {
                    items = cartItems,
                    totalItems = cartItems.Sum(i => i.Quantity),
                    totalPrice = total
                }
            });
        }

        // POST: api/cart/add
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Vérifier si le produit existe
            var product = await _context.Products.FindAsync(request.ProductId);
            if (product == null)
            {
                return NotFound(new { success = false, message = "Product not found" });
            }

            // Vérifier si le produit est déjà dans le panier
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == request.ProductId);

            if (existingItem != null)
            {
                // Augmenter la quantité
                existingItem.Quantity += request.Quantity;
                _context.CartItems.Update(existingItem);
            }
            else
            {
                // Ajouter un nouvel article
                var cartItem = new CartItem
                {
                    UserId = userId,
                    ProductId = request.ProductId,
                    Quantity = request.Quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Product added to cart" });
        }

        // PUT: api/cart/update/{productId}
        [HttpPut("update/{productId}")]
        public async Task<IActionResult> UpdateQuantity(int productId, [FromBody] UpdateQuantityRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (cartItem == null)
            {
                return NotFound(new { success = false, message = "Item not found in cart" });
            }

            if (request.Quantity <= 0)
            {
                // Si quantité = 0, supprimer l'article
                _context.CartItems.Remove(cartItem);
            }
            else
            {
                cartItem.Quantity = request.Quantity;
                _context.CartItems.Update(cartItem);
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Cart updated" });
        }

        // DELETE: api/cart/remove/{productId}
        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (cartItem == null)
            {
                return NotFound(new { success = false, message = "Item not found in cart" });
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Item removed from cart" });
        }

        // DELETE: api/cart/clear
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId)
                .ToListAsync();

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Cart cleared" });
        }

        // POST: api/cart/checkout
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = await _context.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .ToListAsync();

            if (!cartItems.Any())
            {
                return BadRequest(new { success = false, message = "Cart is empty" });
            }

            // Créer une commande pour chaque article
            var orders = new List<Order>();

            foreach (var item in cartItems)
            {
                var order = new Order
                {
                    UserId = userId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    TotalPrice = item.Product.Price * item.Quantity,
                    Status = "pending",
                    CreatedAt = DateTime.Now,
                    StoreId = item.Product.StoreId
                };
                orders.Add(order);
            }

            _context.Orders.AddRange(orders);
            
            // Vider le panier après création des commandes
            _context.CartItems.RemoveRange(cartItems);
            
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Orders created successfully",
                data = new
                {
                    orderCount = orders.Count,
                    orderIds = orders.Select(o => o.Id).ToList()
                }
            });
        }
    }

    // DTOs
    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateQuantityRequest
    {
        public int Quantity { get; set; }
    }
}
