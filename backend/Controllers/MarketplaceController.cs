using backend.DTOs;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using System.Security.Claims;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/market")]
    public class MarketplaceController : ControllerBase
    {
        private readonly Neo4jService _neo4j;
        private readonly UserManager<AppUser> _userManager;

        public MarketplaceController(Neo4jService neo4j, UserManager<AppUser> userManager)
        {
            _neo4j = neo4j;
            _userManager = userManager;
        }

        // GET: api/market/products
        [HttpGet("products")]
        public async Task<IActionResult> GetAllProducts()
        {
            var cypher = @"
                MATCH (p:Product {isActive: true})<-[:HAS_PRODUCT]-(s:Store)
                OPTIONAL MATCH (p)-[:HAS_PROMOTION]->(promo:Promotion)
                WHERE promo.endDate IS NOT NULL AND datetime(promo.endDate) > datetime()
                RETURN p, s.name as storeName, promo
                ORDER BY p.createdAt DESC";
            
            var results = await _neo4j.RunQueryAsync(cypher);
            
            var products = results.Select(r => {
                var pNode = r["p"] as INode;
                var promoNode = r["promo"] as INode;
                
                double price = Convert.ToDouble(pNode.Properties["price"]);
                double discount = 0;
                if (promoNode != null)
                {
                    discount = Convert.ToDouble(promoNode.Properties["discountPercent"]);
                }

                return new
                {
                    id = Convert.ToInt32(pNode.Properties["id"]),
                    name = pNode.Properties["name"].ToString(),
                    price = price,
                    category = pNode.Properties.GetValueOrDefault("category")?.ToString(),
                    imageUrl = pNode.Properties.GetValueOrDefault("imageUrl")?.ToString(),
                    description = pNode.Properties.GetValueOrDefault("description")?.ToString(),
                    stock = Convert.ToInt32(pNode.Properties.GetValueOrDefault("stock") ?? 0),
                    storeName = r["storeName"]?.ToString() ?? "Boutique Inconnue",
                    discount = discount,
                    finalPrice = price * (1 - (discount / 100.0))
                };
            }).ToList();

            return Ok(new { data = products });
        }

        // GET: api/market/promoted
        [HttpGet("promoted")]
        public async Task<IActionResult> GetPromotedProducts()
        {
            var cypher = @"
                MATCH (p:Product {isActive: true})-[:HAS_PROMOTION]->(promo:Promotion)
                WHERE datetime(promo.endDate) > datetime()
                MATCH (p)<-[:HAS_PRODUCT]-(s:Store)
                RETURN p, promo, s.name as storeName";
            
            var results = await _neo4j.RunQueryAsync(cypher);
            
            var promotedProducts = results.Select(r => {
                var pNode = r["p"] as INode;
                var promoNode = r["promo"] as INode;
                
                double price = Convert.ToDouble(pNode.Properties["price"]);
                double discount = Convert.ToDouble(promoNode.Properties["discountPercent"]);

                return new
                {
                    id = Convert.ToInt32(pNode.Properties["id"]),
                    name = pNode.Properties["name"].ToString(),
                    originalPrice = price,
                    imageUrl = pNode.Properties.GetValueOrDefault("imageUrl")?.ToString(),
                    category = pNode.Properties.GetValueOrDefault("category")?.ToString(),
                    promotionTitle = promoNode.Properties["title"].ToString(),
                    discount = discount,
                    finalPrice = price * (1 - (discount / 100.0)),
                    endDate = DateTime.Parse(promoNode.Properties["endDate"].ToString())
                };
            }).ToList();

            return Ok(new { data = promotedProducts });
        }

        // POST: api/market/orders
        [HttpPost("orders")]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            if (dto.Quantity <= 0)
                return BadRequest("La quantité doit être supérieure à 0");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"\n[MARKETPLACE] CreateOrder: Extracted UserId = {userId ?? "NULL"}");

            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("[MARKETPLACE] ❌ No NameIdentifier claim found in token.");
                return Unauthorized();
            }

            // Check if user exists (Handling zombie tokens after DB reset)
            var userCheck = await _neo4j.RunQueryAsync("MATCH (u:User {id: $userId}) RETURN u", new { userId });
            
            if (!userCheck.Any())
            {
                Console.WriteLine($"[MARKETPLACE] ❌ User ID {userId} NOT FOUND in Neo4j!");
                
                // FALLBACK DIAGNOSTIC: Search by email to see if ID changed
                var emailClaim = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Name);
                if (!string.IsNullOrEmpty(emailClaim))
                {
                    var emailCheck = await _neo4j.RunQueryAsync("MATCH (u:User {email: $emailClaim}) RETURN u.id as currentId", new { emailClaim });
                    if (emailCheck.Any())
                    {
                        var dbId = emailCheck.First()["currentId"];
                        Console.WriteLine($"[MARKETPLACE] ⚠️ ID MISMATCH FOUND! DB has ID {dbId} for email {emailClaim}, but token has {userId}");
                    }
                    else
                    {
                        Console.WriteLine($"[MARKETPLACE] ❌ Even email {emailClaim} was not found in database.");
                    }
                }
                
                return Unauthorized("Utilisateur introuvable dans la base. Veuillez vous reconnecter.");
            }


            var cypherCheck = "MATCH (p:Product {id: $productId})<-[:HAS_PRODUCT]-(s:Store) RETURN p, s";
            var checkResult = await _neo4j.RunQueryAsync(cypherCheck, new { productId = dto.ProductId });
            
            if (!checkResult.Any())
                return NotFound("Produit introuvable");

            var pNode = checkResult.First()["p"] as INode;
            var sNode = checkResult.First()["s"] as INode;

            if (!(bool)(pNode.Properties.GetValueOrDefault("isActive") ?? false))
                return BadRequest("Ce produit n'est plus disponible");

            int stock = Convert.ToInt32(pNode.Properties.GetValueOrDefault("stock") ?? 0);
            if (stock < dto.Quantity)
                return BadRequest($"Stock insuffisant. Reste : {stock}");

            var storeIdRaw = sNode.Properties["id"]; // Keep original type (int or string) to ensure MATCH works

            double price = Convert.ToDouble(pNode.Properties["price"]);
            int orderId = new Random().Next(1000, 999999);

            var cypherOrder = @"
                MATCH (u:User {id: $userId})
                MATCH (p:Product {id: $productId})
                MATCH (s:Store {id: $storeId})
                CREATE (o:Order {
                    id: $id,
                    productId: $productId,
                    storeId: $storeId,
                    userId: $userId,
                    quantity: $quantity,
                    totalPrice: $totalPrice,
                    createdAt: $createdAt,
                    status: 'pending'
                })
                CREATE (o)-[:ORDER_BY]->(u)
                CREATE (o)-[:ORDERED_PRODUCT]->(p)
                CREATE (s)-[:HAS_ORDER]->(o)
                SET p.stock = p.stock - $quantity
                RETURN o.id as orderId";
            
            var orderResult = await _neo4j.RunQueryAsync(cypherOrder, new {
                userId,
                productId = dto.ProductId,
                storeId = storeIdRaw, // Pass original type
                id = orderId,
                quantity = dto.Quantity,
                totalPrice = price * dto.Quantity,
                createdAt = DateTime.UtcNow.ToString("O")
            });

            if (!orderResult.Any())
            {
                 return StatusCode(500, "Erreur lors de la création de la commande (echec de liaison). Vérifiez les IDs.");
            }

            // Notification Admin
            var cypherNotif = @"
                CREATE (n:Notification {
                    id: $id,
                    title: $title,
                    message: $message,
                    type: $type,
                    isRead: false,
                    createdAt: $createdAt
                })";
            
            await _neo4j.RunQueryAsync(cypherNotif, new {
                id = Guid.NewGuid().ToString(),
                title = "Nouvelle Commande",
                message = $"Commande #{orderId} reçue pour {sNode.Properties["name"]}. Montant : {price * dto.Quantity} MAD.",
                type = "Admin",
                createdAt = DateTime.UtcNow.ToString("O")
            });

            // Notification Prestataire
            var prestataireId = sNode.Properties["prestataireId"].ToString();
            await _neo4j.RunQueryAsync(@"
                CREATE (n:Notification {
                    id: $id,
                    title: $title,
                    message: $message,
                    type: 'Prestataire',
                    prestataireId: $prestataireId,
                    isRead: false,
                    createdAt: $createdAt
                })", new {
                id = Guid.NewGuid().ToString(),
                title = "Vente Réalisée",
                message = $"Vous avez reçu une nouvelle commande (#{orderId}) pour le produit '{pNode.Properties["name"]}'.",
                prestataireId,
                createdAt = DateTime.UtcNow.ToString("O")
            });

            return Ok(new { message = "Commande créée avec succès", orderId = orderId });
        }

        [HttpPost("track-visit")]
        public async Task<IActionResult> TrackVisit([FromQuery] string type = "Client")
        {
            try
            {
                var cypher = @"
                    CREATE (n:Notification {
                        id: $id,
                        title: $title,
                        message: $message,
                        type: $type,
                        isRead: false,
                        createdAt: $createdAt
                    })";
                
                await _neo4j.RunQueryAsync(cypher, new {
                    id = Guid.NewGuid().ToString(),
                    title = type == "Prestataire" ? "Visite prestataire" : "Visite client",
                    message = "Un utilisateur a consulté la plateforme",
                    type = "Admin",
                    createdAt = DateTime.UtcNow.ToString("O")
                });
                return Ok();
            }
            catch
            {
                return Ok();
            }
        }

        [HttpGet("my-orders")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var cypher = @"
                MATCH (o:Order)-[:ORDER_BY]->(u:User {id: $userId})
                MATCH (o)-[:ORDERED_PRODUCT]->(p:Product)<-[:HAS_PRODUCT]-(s:Store)
                RETURN o, p, s.name as storeName
                ORDER BY o.createdAt DESC";
            
            var results = await _neo4j.RunQueryAsync(cypher, new { userId });
            
            var orders = results.Select(r => {
                var oNode = r["o"] as INode;
                var pNode = r["p"] as INode;
                return new
                {
                    id = Convert.ToInt32(oNode.Properties["id"]),
                    productId = Convert.ToInt32(oNode.Properties["productId"]),
                    ProductName = pNode.Properties["name"].ToString(),
                    ProductImage = pNode.Properties.GetValueOrDefault("imageUrl")?.ToString(),
                    StoreName = r["storeName"]?.ToString(),
                    quantity = Convert.ToInt32(oNode.Properties["quantity"]),
                    totalPrice = Convert.ToDouble(oNode.Properties["totalPrice"]),
                    status = oNode.Properties["status"].ToString(),
                    createdAt = DateTime.Parse(oNode.Properties["createdAt"].ToString())
                };
            }).ToList();

            return Ok(new { data = orders });
        }

        [HttpPost("orders/{id}/pay")]
        [Authorize]
        public async Task<IActionResult> PayOrder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cypher = @"
                MATCH (o:Order {id: $id})-[:ORDER_BY]->(u:User {id: $userId})
                WHERE o.status IN ['confirmed', 'pending']
                SET o.status = 'processing'
                RETURN o.status as status";
            
            var results = await _neo4j.RunQueryAsync(cypher, new { userId, id });
            
            if (!results.Any())
                return NotFound("Commande introuvable ou non payable.");

            return Ok(new { message = "Paiement réussi", status = results.First()["status"] });
        }

        [HttpDelete("orders/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var cypherCheck = "MATCH (o:Order {id: $id})-[:ORDER_BY]->(u:User {id: $userId}) RETURN o.status as status";
            var check = await _neo4j.RunQueryAsync(cypherCheck, new { userId, id });
            
            if (!check.Any())
                return NotFound("Commande introuvable.");

            var status = check.First()["status"].ToString();
            if (status != "pending" && status != "confirmed")
                return BadRequest("Impossible de supprimer une commande en cours de traitement ou déjà payée.");

            var cypherDelete = "MATCH (o:Order {id: $id}) DETACH DELETE o";
            await _neo4j.RunQueryAsync(cypherDelete, new { id });

            return Ok(new { message = "Commande supprimée avec succès" });
        }
    }
}
