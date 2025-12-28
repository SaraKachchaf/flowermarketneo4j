using backend.Models;
using backend.Admin.Dto;
using Microsoft.AspNetCore.Identity;
using Neo4j.Driver;

namespace backend.Admin
{
    public class AdminService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly Neo4jService _neo4j;

        public AdminService(
            UserManager<AppUser> userManager,
            Neo4jService neo4j)
        {
            _userManager = userManager;
            _neo4j = neo4j;
        }

        // =======================
        // STATISTIQUES ADMIN
        // =======================
        public async Task<AdminStatsDto> GetStatistics()
        {
            var cypherUsers = "MATCH (u:User)-[:HAS_ROLE]->(r:Role) RETURN u, r.name as roleName";
            var userResults = await _neo4j.RunQueryAsync(cypherUsers);
            
            int totalClients = 0;
            int totalPrestataires = 0;
            int pendingPrestataires = 0;

            foreach (var r in userResults)
            {
                var role = r["roleName"].ToString();
                if (role == "Client") totalClients++;
                else if (role == "Prestataire")
                {
                    totalPrestataires++;
                    var uNode = r["u"] as INode;
                    if (!(bool)(uNode.Properties.GetValueOrDefault("isApproved") ?? false))
                        pendingPrestataires++;
                }
            }

            var cypherGlobal = @"
                OPTIONAL MATCH (p:Product)
                OPTIONAL MATCH (o:Order)
                RETURN count(DISTINCT p) as totalProducts, 
                       count(DISTINCT o) as totalOrders, 
                       sum(o.totalPrice) as totalRevenue";
            
            var globalResults = await _neo4j.RunQueryAsync(cypherGlobal);
            var g = globalResults.First();

            return new AdminStatsDto
            {
                TotalClients = totalClients,
                TotalPrestataires = totalPrestataires,
                PendingPrestataires = pendingPrestataires,
                TotalProducts = Convert.ToInt32(g["totalProducts"]),
                TotalOrders = Convert.ToInt32(g["totalOrders"]),
                TotalRevenue = Convert.ToDecimal(g["totalRevenue"] ?? 0),
                PendingOrders = Convert.ToInt32(g["totalOrders"]) // Placeholder for actual pending logic
            };
        }

        // =======================
        // UTILISATEURS
        // =======================
        public async Task<List<UserDto>> GetAllUsers()
        {
            var cypher = @"
                MATCH (u:User)-[:HAS_ROLE]->(r:Role)
                OPTIONAL MATCH (s:Store {prestataireId: u.id})
                RETURN u, r.name as role, s.name as storeName";
            
            var results = await _neo4j.RunQueryAsync(cypher);
            
            return results.Select(r => {
                var uNode = r["u"] as INode;
                return new UserDto
                {
                    Id = uNode.Properties["id"].ToString(),
                    FullName = uNode.Properties["fullName"].ToString(),
                    Email = uNode.Properties["email"].ToString(),
                    Role = r["role"].ToString(),
                    IsApproved = (bool)(uNode.Properties.GetValueOrDefault("isApproved") ?? false),
                    CreatedAt = DateTime.Parse(uNode.Properties["createdAt"].ToString()),
                    StoreName = r["storeName"]?.ToString()
                };
            }).ToList();
        }

        // =======================
        // PRESTATAIRES
        // =======================
        public async Task<List<UserDto>> GetPrestataires()
        {
            var cypher = @"
                MATCH (u:User)-[:HAS_ROLE]->(r:Role {normalizedName: 'PRESTATAIRE'})
                OPTIONAL MATCH (s:Store {prestataireId: u.id})
                RETURN u, s.name as storeName";
            
            var results = await _neo4j.RunQueryAsync(cypher);
            
            return results.Select(r => {
                var uNode = r["u"] as INode;
                return new UserDto
                {
                    Id = uNode.Properties["id"].ToString(),
                    FullName = uNode.Properties["fullName"].ToString(),
                    Email = uNode.Properties["email"].ToString(),
                    Role = "Prestataire",
                    IsApproved = (bool)(uNode.Properties.GetValueOrDefault("isApproved") ?? false),
                    CreatedAt = DateTime.Parse(uNode.Properties["createdAt"].ToString()),
                    StoreName = r["storeName"]?.ToString()
                };
            }).ToList();
        }

        // =======================
        // PRODUITS
        // =======================
        public async Task<List<ProductDto>> GetAllProducts()
        {
            var cypher = @"
                MATCH (p:Product)<-[:HAS_PRODUCT]-(s:Store)
                RETURN p, s.name as storeName";
            
            var results = await _neo4j.RunQueryAsync(cypher);
            
            return results.Select(r => {
                var pNode = r["p"] as INode;
                return new ProductDto
                {
                    Id = Convert.ToInt32(pNode.Properties["id"]),
                    Name = pNode.Properties["name"].ToString(),
                    Price = Convert.ToDecimal(pNode.Properties["price"]),
                    ImageUrl = pNode.Properties.GetValueOrDefault("imageUrl")?.ToString(),
                    StoreName = r["storeName"]?.ToString() ?? "Boutique inconnue",
                    Category = pNode.Properties.GetValueOrDefault("category")?.ToString() ?? "Non catégorisé",
                    Stock = Convert.ToInt32(pNode.Properties.GetValueOrDefault("stock") ?? 0),
                    PrestataireName = "Inconnu",
                    CreatedAt = DateTime.Parse(pNode.Properties["createdAt"].ToString())
                };
            }).ToList();
        }

        // =======================
        // COMMANDES
        // =======================
        public async Task<List<OrderDto>> GetAllOrders()
        {
            var cypher = @"
                MATCH (o:Order)-[:ORDER_BY]->(u:User)
                MATCH (o)-[:ORDERED_PRODUCT]->(p:Product)
                RETURN o, u, p";
            
            var results = await _neo4j.RunQueryAsync(cypher);
            
            return results.Select(r => {
                var oNode = r["o"] as INode;
                var uNode = r["u"] as INode;
                var pNode = r["p"] as INode;
                return new OrderDto
                {
                    Id = Convert.ToInt32(oNode.Properties["id"]),
                    ProductName = pNode.Properties["name"].ToString(),
                    Quantity = Convert.ToInt32(oNode.Properties["quantity"]),
                    TotalPrice = Convert.ToDecimal(oNode.Properties["totalPrice"]),
                    Status = oNode.Properties["status"].ToString(),
                    CustomerName = uNode.Properties["fullName"].ToString(),
                    CustomerEmail = uNode.Properties["email"].ToString(),
                    OrderDate = DateTime.Parse(oNode.Properties["createdAt"].ToString())
                };
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
            if (user == null) return false;

            user.IsApproved = true;
            await _userManager.UpdateAsync(user);

            var cypher = @"
                MERGE (s:Store {prestataireId: $userId})
                ON CREATE SET s.id = $id,
                              s.name = $name,
                              s.description = $description,
                              s.address = $address
                RETURN s";
            
            await _neo4j.RunQueryAsync(cypher, new {
                userId,
                id = new Random().Next(1000, 999999),
                name = $"Boutique de {user.FullName}",
                description = "Description de la boutique",
                address = "Adresse à définir"
            });

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
            // Filtrer explicitement les notifications pour l'Admin
            var cypher = "MATCH (n:Notification) WHERE n.type = 'Admin' RETURN n ORDER BY n.createdAt DESC LIMIT 20";
            var results = await _neo4j.RunQueryAsync(cypher);
            return results.Select(r => {
                var nNode = r["n"] as INode;
                
                // Helper robuste pour extraire les propriétés sans souci de casse
                string GetProp(string key) {
                    if (nNode.Properties.ContainsKey(key)) return nNode.Properties[key]?.ToString();
                    var pascalKey = char.ToUpper(key[0]) + key.Substring(1);
                    if (nNode.Properties.ContainsKey(pascalKey)) return nNode.Properties[pascalKey]?.ToString();
                    return null;
                }

                bool GetBool(string key) {
                    if (nNode.Properties.ContainsKey(key)) return (bool)nNode.Properties[key];
                    var pascalKey = char.ToUpper(key[0]) + key.Substring(1);
                    if (nNode.Properties.ContainsKey(pascalKey)) return (bool)nNode.Properties[pascalKey];
                    return false;
                }

                // Récupération de l'ID avec fallback si absent (pour éviter //read 404)
                var id = GetProp("id");
                if (string.IsNullOrEmpty(id)) 
                    id = "missing_" + Guid.NewGuid().ToString().Substring(0, 8);

                return new Notification
                {
                    Id = id,
                    Title = GetProp("title") ?? GetProp("Title") ?? "Notification Admin",
                    Message = GetProp("message") ?? GetProp("Message") ?? "Consulter le détail dans les commandes.",
                    Type = GetProp("type") ?? "Admin",
                    IsRead = GetBool("isRead"),
                    CreatedAt = DateTime.Parse(GetProp("createdAt") ?? GetProp("CreatedAt") ?? DateTime.UtcNow.ToString("O"))
                };
            }).ToList();
        }

        public async Task<bool> MarkNotificationAsRead(string id)
        {
            var cypher = "MATCH (n:Notification {id: $id}) SET n.isRead = true RETURN count(n) > 0 as updated";
            var results = await _neo4j.RunQueryAsync(cypher, new { id });
            return (bool)results.First()["updated"];
        }
        public async Task ExecuteRawQuery(string cypher)
        {
            await _neo4j.RunQueryAsync(cypher);
        }
    }
}
