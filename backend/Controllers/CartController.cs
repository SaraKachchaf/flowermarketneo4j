using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using backend.Models;
using Neo4j.Driver;
using System.Security.Claims;

namespace backend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly Neo4jService _neo4j;

        public CartController(Neo4jService neo4j)
        {
            _neo4j = neo4j;
        }

        // GET: api/cart
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cypher = @"
                MATCH (u:User {id: $userId})-[:HAS_CART_ITEM]->(c:CartItem)
                MATCH (c)-[:CART_PRODUCT]->(p:Product)<-[:HAS_PRODUCT]-(s:Store)-[:OWNED_BY]->(prest:User)
                RETURN c, p, s, prest
                ORDER BY c.addedAt DESC";
            
            var results = await _neo4j.RunQueryAsync(cypher, new { userId });
            
            var cartItems = results.Select(r => {
                var cNode = r["c"] as INode;
                var pNode = r["p"] as INode;
                var sNode = r["s"] as INode;
                var prestNode = r["prest"] as INode;

                double price = Convert.ToDouble(pNode.Properties["price"]);
                int quantity = Convert.ToInt32(cNode.Properties["quantity"]);

                return new
                {
                    id = Convert.ToInt32(cNode.Properties["id"]),
                    productId = Convert.ToInt32(pNode.Properties["id"]),
                    productName = pNode.Properties["name"].ToString(),
                    productPrice = price,
                    productImage = pNode.Properties.GetValueOrDefault("imageUrl")?.ToString(),
                    quantity = quantity,
                    totalPrice = price * quantity,
                    storeName = sNode.Properties["name"].ToString(),
                    storeId = Convert.ToInt32(sNode.Properties["id"]),
                    storeAddress = sNode.Properties.GetValueOrDefault("address")?.ToString(),
                    storeCity = sNode.Properties.GetValueOrDefault("address")?.ToString(),
                    storeEmail = prestNode.Properties["email"].ToString(),
                    addedAt = DateTime.Parse(cNode.Properties["addedAt"].ToString())
                };
            }).ToList();

            return Ok(new
            {
                success = true,
                data = new
                {
                    items = cartItems,
                    totalItems = cartItems.Sum(i => i.quantity),
                    totalPrice = cartItems.Sum(item => item.totalPrice)
                }
            });
        }

        // POST: api/cart/add
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cypherCheck = "MATCH (p:Product {id: $productId}) RETURN p";
            var productFound = await _neo4j.RunQueryAsync(cypherCheck, new { productId = request.ProductId });
            if (!productFound.Any())
                return NotFound(new { success = false, message = "Product not found" });

            var cypher = @"
                MATCH (u:User {id: $userId})
                MATCH (p:Product {id: $productId})
                MERGE (u)-[:HAS_CART_ITEM]->(c:CartItem {productId: $productId, userId: $userId})
                ON CREATE SET c.id = $id,
                              c.quantity = $quantity,
                              c.addedAt = $now
                ON MATCH SET c.quantity = c.quantity + $quantity
                MERGE (c)-[:CART_PRODUCT]->(p)
                RETURN c";
            
            await _neo4j.RunQueryAsync(cypher, new {
                userId,
                productId = request.ProductId,
                id = new Random().Next(1000, 999999),
                quantity = request.Quantity,
                now = DateTime.UtcNow.ToString("O")
            });

            return Ok(new { success = true, message = "Product added to cart" });
        }

        // PUT: api/cart/update/{productId}
        [HttpPut("update/{productId}")]
        public async Task<IActionResult> UpdateQuantity(int productId, [FromBody] UpdateQuantityRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (request.Quantity <= 0)
            {
                var cypherDelete = "MATCH (u:User {id: $userId})-[:HAS_CART_ITEM]->(c:CartItem {productId: $productId}) DETACH DELETE c";
                await _neo4j.RunQueryAsync(cypherDelete, new { userId, productId });
            }
            else
            {
                var cypherUpdate = @"
                    MATCH (u:User {id: $userId})-[:HAS_CART_ITEM]->(c:CartItem {productId: $productId})
                    SET c.quantity = $quantity
                    RETURN count(c) > 0 as updated";
                var res = await _neo4j.RunQueryAsync(cypherUpdate, new { userId, productId, quantity = request.Quantity });
                if (!(bool)res.First()["updated"])
                    return NotFound(new { success = false, message = "Item not found in cart" });
            }

            return Ok(new { success = true, message = "Cart updated" });
        }

        // DELETE: api/cart/remove/{productId}
        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cypherDelete = "MATCH (u:User {id: $userId})-[:HAS_CART_ITEM]->(c:CartItem {productId: $productId}) DETACH DELETE c RETURN count(c) > 0 as deleted";
            // In Neo4j DETACH DELETE count(c) might be tricky if it's already deleted in the same query? No, it's fine.
            // Actually count(c) is better before delete.
            
            var cypher = @"
                MATCH (u:User {id: $userId})-[:HAS_CART_ITEM]->(c:CartItem {productId: $productId})
                WITH c
                DETACH DELETE c
                RETURN count(c) > 0 as deleted";
            
            await _neo4j.RunQueryAsync(cypher, new { userId, productId });
            return Ok(new { success = true, message = "Item removed from cart" });
        }

        // DELETE: api/cart/clear
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cypher = "MATCH (u:User {id: $userId})-[:HAS_CART_ITEM]->(c:CartItem) DETACH DELETE c";
            await _neo4j.RunQueryAsync(cypher, new { userId });
            return Ok(new { success = true, message = "Cart cleared" });
        }

        // POST: api/cart/checkout
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cypherFetch = @"
                MATCH (u:User {id: $userId})-[:HAS_CART_ITEM]->(c:CartItem)
                MATCH (c)-[:CART_PRODUCT]->(p:Product)
                RETURN c, p";
            
            var results = await _neo4j.RunQueryAsync(cypherFetch, new { userId });
            if (!results.Any())
                return BadRequest(new { success = false, message = "Cart is empty" });

            var orderIds = new List<int>();
            foreach (var r in results)
            {
                var cNode = r["c"] as INode;
                var pNode = r["p"] as INode;
                
                int orderId = new Random().Next(1000, 999999);
                orderIds.Add(orderId);

                var cypherOrder = @"
                    MATCH (u:User {id: $userId})
                    MATCH (p:Product {id: $productId})<-[:HAS_PRODUCT]-(s:Store)
                    CREATE (o:Order {
                        id: $id,
                        userId: $userId,
                        productId: $productId,
                        quantity: $quantity,
                        totalPrice: $totalPrice,
                        status: 'pending',
                        createdAt: $now,
                        storeId: s.id
                    })
                    CREATE (u)-[:ORDER_BY]->(o)
                    CREATE (o)-[:ORDERED_PRODUCT]->(p)
                    CREATE (s)-[:HAS_ORDER]->(o)
                    RETURN o.id";
                
                await _neo4j.RunQueryAsync(cypherOrder, new {
                    userId,
                    productId = Convert.ToInt32(pNode.Properties["id"]),
                    id = orderId,
                    quantity = Convert.ToInt32(cNode.Properties["quantity"]),
                    totalPrice = Convert.ToDouble(pNode.Properties["price"]) * Convert.ToInt32(cNode.Properties["quantity"]),
                    now = DateTime.Now.ToString("O")
                });
            }

            // Clear cart
            await ClearCart();

            return Ok(new
            {
                success = true,
                message = "Orders created successfully",
                data = new
                {
                    orderCount = orderIds.Count,
                    orderIds = orderIds
                }
            });
        }
    }

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
