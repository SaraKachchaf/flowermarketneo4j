using backend.Admin.Dto;
using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Admin
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;
        private readonly FlowerMarketDbContext _context;

        public AdminController(
            AdminService adminService,
            FlowerMarketDbContext context
        )
        {
            _adminService = adminService;
            _context = context;
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
        public async Task<IActionResult> MarkNotificationAsRead(Guid id)
        {
            var success = await _adminService.MarkNotificationAsRead(id);
            if (!success) return NotFound();
            
            return Ok(new { success = true });
        }



    }
}
