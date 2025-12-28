using backend.Admin.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Admin
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;

        public AdminController(AdminService adminService)
        {
            _adminService = adminService;
        }

        // ✅ TEST TOKEN ADMIN (IMPORTANT)
        // GET: api/admin/me
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                success = true,
                adminEmail = User.Identity?.Name,
                roles = User.Claims
                    .Where(c => c.Type.Contains("role"))
                    .Select(c => c.Value)
            });
        }

        // ======================
        // STATS
        // ======================
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var stats = await _adminService.GetStatistics();
                return Ok(new
                {
                    success = true,
                    data = stats
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erreur lors de la récupération des statistiques",
                    error = ex.Message
                });
            }
        }

        // ======================
        // USERS
        // ======================
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _adminService.GetAllUsers();
                return Ok(new
                {
                    success = true,
                    count = users.Count,
                    data = users
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erreur lors de la récupération des utilisateurs",
                    error = ex.Message
                });
            }
        }

        // DELETE: api/admin/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var success = await _adminService.DeleteUser(id);
                if (!success)
                    return NotFound(new
                    {
                        success = false,
                        message = "Utilisateur non trouvé"
                    });

                return Ok(new
                {
                    success = true,
                    message = "Utilisateur supprimé avec succès"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erreur lors de la suppression de l'utilisateur",
                    error = ex.Message
                });
            }
        }

        // ======================
        // PRODUCTS
        // ======================
        [HttpGet("products")]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                var products = await _adminService.GetAllProducts();
                return Ok(new
                {
                    success = true,
                    count = products.Count,
                    data = products
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erreur lors de la récupération des produits",
                    error = ex.Message
                });
            }
        }

        // ======================
        // ORDERS
        // ======================
        [HttpGet("orders")]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                var orders = await _adminService.GetAllOrders();
                return Ok(new
                {
                    success = true,
                    count = orders.Count,
                    data = orders
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erreur lors de la récupération des commandes",
                    error = ex.Message
                });
            }
        }

        // ======================
        // PRESTATAIRES
        // ======================
        [HttpGet("prestataires")]
        public async Task<IActionResult> GetPrestataires()
        {
            try
            {
                var prestataires = await _adminService.GetPrestataires();
                return Ok(new
                {
                    success = true,
                    count = prestataires.Count,
                    data = prestataires
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erreur lors de la récupération des prestataires",
                    error = ex.Message
                });
            }
        }

        // POST: api/admin/prestataires/{id}/approve
        [HttpPost("prestataires/{id}/approve")]
        public async Task<IActionResult> ApprovePrestataire(string id)
        {
            try
            {
                var success = await _adminService.ApprovePrestataire(id);
                if (!success)
                    return NotFound(new
                    {
                        success = false,
                        message = "Prestataire non trouvé"
                    });

                return Ok(new
                {
                    success = true,
                    message = "Prestataire approuvé avec succès"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erreur lors de l'approbation du prestataire",
                    error = ex.Message
                });
            }
        }

        // DELETE: api/admin/prestataires/{id}/reject
        [HttpDelete("prestataires/{id}/reject")]
        public async Task<IActionResult> RejectPrestataire(string id)
        {
            try
            {
                var success = await _adminService.RejectPrestataire(id);
                if (!success)
                    return NotFound(new
                    {
                        success = false,
                        message = "Prestataire non trouvé"
                    });

                return Ok(new
                {
                    success = true,
                    message = "Prestataire rejeté avec succès"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erreur lors du rejet du prestataire",
                    error = ex.Message
                });
            }
        }
        // ======================
        // NOTIFICATIONS ADMIN
        // ======================
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var notifications = await _adminService.GetLastNotifications();
            return Ok(new { data = notifications });
        }

        [HttpPut("notifications/{id}/read")]
        public async Task<IActionResult> MarkNotificationAsRead(string id)
        {
            var success = await _adminService.MarkNotificationAsRead(id);
            if (!success) return NotFound();
            
            return Ok(new { success = true });
        }

        // ======================
        // DATA FIX / MIGRATION
        // ======================
        [HttpPost("fix-data")]
        public async Task<IActionResult> FixData()
        {
            try
            {
                // 0. Normalize existing notification properties and ensure visibility
                await _adminService.ExecuteRawQuery(@"
                    MATCH (n:Notification)
                    SET n.title = coalesce(n.title, n.Title, 'Notification'),
                        n.message = coalesce(n.message, n.Message, 'Aucun détail disponible'),
                        n.type = coalesce(n.type, n.Type, 'Admin'),
                        n.id = coalesce(n.id, n.Id, 'notif_' + toString(n.createdAt)),
                        n.isRead = coalesce(n.isRead, n.IsRead, false),
                        n.createdAt = coalesce(n.createdAt, n.CreatedAt, datetime().epochMillis)
                    REMOVE n.Id, n.Title, n.Message, n.Type, n.IsRead, n.CreatedAt");

                // 1. Fix ORDER_BY direction (should be Order -> User)
                await _adminService.ExecuteRawQuery("MATCH (u:User)-[r:ORDER_BY]->(o:Order) CREATE (o)-[:ORDER_BY]->(u) DELETE r");
                
                // 2. Fix Relationship names (HAS_ITEM -> ORDERED_PRODUCT)
                await _adminService.ExecuteRawQuery("MATCH (o:Order)-[r:HAS_ITEM]->(p:Product) CREATE (o)-[:ORDERED_PRODUCT]->(p) DELETE r");
                
                // 3. Fix missing Store links
                await _adminService.ExecuteRawQuery(@"
                    MATCH (o:Order)
                    MATCH (p:Product {id: o.productId})<-[:HAS_PRODUCT]-(s:Store)
                    MERGE (s)-[:HAS_ORDER]->(o)");

                // 4. Create missing notifications for existing orders
                // Admin Notifications
                await _adminService.ExecuteRawQuery(@"
                    MATCH (o:Order)
                    MATCH (p:Product {id: o.productId})<-[:HAS_PRODUCT]-(s:Store)
                    WHERE NOT EXISTS { MATCH (n:Notification {type: 'Admin'}) WHERE n.message CONTAINS ('Commande #' + toString(o.id)) }
                    CREATE (n:Notification {
                        id: apoc.create.uuid(),
                        title: 'Commande Existante',
                        message: 'Commande #' + toString(o.id) + ' pour ' + s.name + '. Montant : ' + toString(o.totalPrice) + ' MAD.',
                        type: 'Admin',
                        isRead: false,
                        createdAt: o.createdAt
                    })");

                // Prestataire Notifications
                await _adminService.ExecuteRawQuery(@"
                    MATCH (o:Order)
                    MATCH (p:Product {id: o.productId})<-[:HAS_PRODUCT]-(s:Store)
                    WHERE NOT EXISTS { MATCH (n:Notification {type: 'Prestataire'}) WHERE n.message CONTAINS ('commande (#' + toString(o.id) + ')') }
                    CREATE (n:Notification {
                        id: apoc.create.uuid(),
                        title: 'Vente Passée',
                        message: 'Notification de migration : commande (#' + toString(o.id) + ') pour le produit ' + p.name,
                        type: 'Prestataire',
                        prestataireId: s.prestataireId,
                        isRead: false,
                        createdAt: o.createdAt
                    })");

                return Ok(new { success = true, message = "Data migration and notification backfill completed successfully." });
            }
            catch (Exception ex)
            {
                // Fallback if apoc.create.uuid() is not available
                if (ex.Message.Contains("apoc.create.uuid"))
                {
                     try {
                         await _adminService.ExecuteRawQuery(@"
                            MATCH (o:Order)
                            MATCH (p:Product {id: o.productId})<-[:HAS_PRODUCT]-(s:Store)
                            WHERE NOT EXISTS { MATCH (n:Notification {type: 'Admin'}) WHERE n.message CONTAINS ('Commande #' + toString(o.id)) }
                            CREATE (n:Notification {
                                id: toString(o.id) + '_admin',
                                title: 'Commande Existante',
                                message: 'Commande #' + toString(o.id) + ' pour ' + s.name + '. Montant : ' + toString(o.totalPrice) + ' MAD.',
                                type: 'Admin',
                                isRead: false,
                                createdAt: o.createdAt
                            })");
                         return Ok(new { success = true, message = "Data migration completed with simplified IDs (APOC missing)." });
                     } catch(Exception ex2) {
                         return StatusCode(500, new { success = false, message = ex2.Message });
                     }
                }
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
