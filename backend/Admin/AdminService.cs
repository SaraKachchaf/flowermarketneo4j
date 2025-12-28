using backend.Models;
using backend.Admin.Dto;
using backend.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace backend.Admin
{
    public class AdminService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly FlowerMarketDbContext _context;

        public AdminService(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            FlowerMarketDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // =======================
        // STATISTIQUES ADMIN
        // =======================
        public async Task<AdminStatsDto> GetStatistics()
        {
            var users = await _userManager.Users.ToListAsync();

            int totalClients = 0;
            int totalPrestataires = 0;
            int pendingPrestataires = 0;

            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, "Client"))
                    totalClients++;

                if (await _userManager.IsInRoleAsync(user, "Prestataire"))
                {
                    totalPrestataires++;
                    if (!user.IsApproved)
                        pendingPrestataires++;
                }
            }

            return new AdminStatsDto
            {
                TotalClients = totalClients,
                TotalPrestataires = totalPrestataires,
                PendingPrestataires = pendingPrestataires,
                TotalProducts = await _context.Products.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalRevenue = await _context.Orders.SumAsync(o => (decimal?)o.TotalPrice) ?? 0,
                PendingOrders = await _context.Orders.CountAsync()
            };
        }

        // =======================
        // UTILISATEURS
        // =======================
        public async Task<List<UserDto>> GetAllUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var stores = await _context.Stores.ToListAsync();
            var storeLookup = stores.ToDictionary(s => s.PrestataireId, s => s.Name);
            
            var result = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                storeLookup.TryGetValue(user.Id, out var storeName);

                result.Add(new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = roles.FirstOrDefault() ?? "Client",
                    IsApproved = user.IsApproved,
                    CreatedAt = user.CreatedAt,
                    StoreName = storeName
                });
            }

            return result;
        }

        // =======================
        // PRESTATAIRES
        // =======================
        public async Task<List<UserDto>> GetPrestataires()
        {
            var prestataires = await _userManager.GetUsersInRoleAsync("Prestataire");
            var stores = await _context.Stores.ToListAsync();
            var storeLookup = stores.ToDictionary(s => s.PrestataireId, s => s.Name);

            return prestataires.Select(p => 
            {
                storeLookup.TryGetValue(p.Id, out var storeName);
                return new UserDto
                {
                    Id = p.Id,
                    FullName = p.FullName,
                    Email = p.Email,
                    Role = "Prestataire",
                    IsApproved = p.IsApproved,
                    CreatedAt = DateTime.Now, // ✅ SAFE
                    StoreName = storeName
                };
            }).ToList();
        }

        // =======================
        // PRODUITS
        // =======================
        public async Task<List<ProductDto>> GetAllProducts()
        {
            var products = await _context.Products
                .Include(p => p.Store)
                .ToListAsync();

            return products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = (decimal)p.Price,
                ImageUrl = p.ImageUrl,
                StoreName = p.Store != null ? p.Store.Name : "Boutique inconnue",
                Category = p.Category ?? "Non catégorisé",
                Stock = p.Stock,
                PrestataireName = "Inconnu", // ❗ relation inexistante
                CreatedAt = p.CreatedAt
            }).ToList();
        }

        // =======================
        // COMMANDES
        // =======================
        public async Task<List<OrderDto>> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.Product)
                .Include(o => o.User)
                .ToListAsync();

            return orders.Select(o => new OrderDto
            {
                Id = o.Id,
                ProductName = o.Product != null ? o.Product.Name : "Produit supprimé",
                Quantity = o.Quantity,
                TotalPrice = (decimal)o.TotalPrice,
                Status = o.Status ?? "En attente",
                CustomerName = o.User != null ? o.User.FullName : "Client inconnu",
                CustomerEmail = o.User != null ? o.User.Email : "Email inconnu",
                OrderDate = o.CreatedAt
            }).ToList();
        }

        // =======================
        // SUPPRIMER UTILISATEUR
        // =======================
        public async Task<bool> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        // =======================
        // APPROUVER PRESTATAIRE
        // =======================
        public async Task<bool> ApprovePrestataire(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            user.IsApproved = true;
            await _userManager.UpdateAsync(user);

            // ✅ CRÉATION AUTOMATIQUE DU STORE
            var storeExists = await _context.Stores
                .AnyAsync(s => s.PrestataireId == userId);

            if (!storeExists)
            {
                var store = new Store
                {
                    Name = $"Boutique de {user.FullName}",
                    Description = "Description de la boutique",
                    Address = "Adresse à définir",
                    PrestataireId = userId
                };

                _context.Stores.Add(store);
                await _context.SaveChangesAsync();
            }

            return true;
        }

        // =======================
        // REJETER PRESTATAIRE
        // =======================
        public async Task<bool> RejectPrestataire(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }
        public async Task<List<Notification>> GetLastNotifications()
        {
            return await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();
        }

        public async Task<bool> MarkNotificationAsRead(Guid id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

    }

}
