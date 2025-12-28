using backend.Models;
using backend.Prestataire.Dto;
using Neo4j.Driver;

namespace backend.Prestataire
{
    public class PrestataireService
    {
        private readonly Neo4jService _neo4j;

        public PrestataireService(Neo4jService neo4j)
        {
            _neo4j = neo4j;
        }

        // ---------------------------------------------------
        // 1. Récupérer la boutique du prestataire connecté
        // ---------------------------------------------------
        public async Task<Store?> GetMyStore(string prestataireId)
        {
            var cypher = @"
                MATCH (s:Store {prestataireId: $prestataireId})
                OPTIONAL MATCH (s)-[:HAS_PRODUCT]->(p:Product)
                RETURN s, collect(p) as products";
            
            var results = await _neo4j.RunQueryAsync(cypher, new { prestataireId });
            var first = results.FirstOrDefault();
            if (first == null) return null;

            var storeNode = first["s"] as INode;
            var productNodes = first["products"] as List<object>;

            var store = MapToStore(storeNode);
            if (store != null && productNodes != null)
            {
                foreach (var pObj in productNodes)
                {
                    var pNode = pObj as INode;
                    if (pNode != null) store.Products.Add(MapToProduct(pNode));
                }
            }
            return store;
        }

        // ---------------------------------------------------
        // 2. Créer ou modifier la boutique
        // ---------------------------------------------------
        public async Task<Store> CreateOrUpdateStore(string prestataireId, CreateStoreDto dto)
        {
            var cypher = @"
                MERGE (s:Store {prestataireId: $prestataireId})
                ON CREATE SET s.id = apoc.create.uuid(), 
                              s.name = $name, 
                              s.description = $description, 
                              s.address = $address
                ON MATCH SET s.name = $name, 
                             s.description = $description, 
                             s.address = $address
                RETURN s";
            
            // Note: apoc.create.uuid() requires APOC. If not available, we handle it in C#.
            // Let's use a simpler approach if APOC is not guaranteed.
            
            var existingStore = await GetMyStore(prestataireId);
            string id = existingStore?.Id.ToString() ?? new Random().Next(1000, 999999).ToString(); // Fallback for int Id

            cypher = @"
                MERGE (s:Store {prestataireId: $prestataireId})
                SET s.name = $name,
                    s.description = $description,
                    s.address = $address,
                    s.id = coalesce(s.id, $id)
                RETURN s";

            var results = await _neo4j.RunQueryAsync(cypher, new 
            { 
                prestataireId, 
                name = dto.Name, 
                description = dto.Description, 
                address = dto.Address,
                id = int.Parse(id)
            });

            return MapToStore(results.First()["s"] as INode);
        }

        // ---------------------------------------------------
        // 3. Ajouter un produit
        // ---------------------------------------------------
        public async Task<Product?> AddProduct(string prestataireId, CreateProductDto dto)
        {
            var store = await GetMyStore(prestataireId);
            if (store == null) 
            {
               // Auto-create store if it doesn't exist
               store = await CreateOrUpdateStore(prestataireId, new CreateStoreDto 
               { 
                   Name = "Ma Boutique", 
                   Description = "Bienvenue dans ma boutique", 
                   Address = "Non spécifiée" 
               });
            }

            var cypher = @"
                MATCH (s:Store {prestataireId: $prestataireId})
                CREATE (p:Product {
                    id: $id,
                    name: $name,
                    price: $price,
                    imageUrl: $imageUrl,
                    storeId: $storeId,
                    createdAt: $createdAt,
                    isActive: true,
                    stock: $stock,
                    category: $category,
                    description: $description
                })
                CREATE (s)-[:HAS_PRODUCT]->(p)
                RETURN p";

            int productId = new Random().Next(1000, 999999);

            var results = await _neo4j.RunQueryAsync(cypher, new 
            { 
                prestataireId,
                id = productId,
                name = dto.Name,
                price = dto.Price,
                imageUrl = dto.ImageUrl,
                storeId = store.Id,
                createdAt = DateTime.UtcNow.ToString("O"),
                stock = dto.Stock,
                category = dto.Category,
                description = dto.Description
            });

            return MapToProduct(results.First()["p"] as INode);
        }

        // ---------------------------------------------------
        // 4. Modifier un produit
        // ---------------------------------------------------
        public async Task<bool> UpdateProduct(int id, string prestataireId, Product dto)
        {
            var cypher = @"
                MATCH (s:Store {prestataireId: $prestataireId})-[:HAS_PRODUCT]->(p:Product {id: $id})
                SET p.name = $name,
                    p.price = $price,
                    p.imageUrl = $imageUrl,
                    p.stock = $stock,
                    p.category = $category,
                    p.description = $description,
                    p.isActive = $isActive
                RETURN count(p) > 0 as updated";
            
            var results = await _neo4j.RunQueryAsync(cypher, new 
            { 
                id,
                prestataireId,
                name = dto.Name, 
                price = dto.Price, 
                imageUrl = dto.ImageUrl,
                stock = dto.Stock,
                category = dto.Category,
                description = dto.Description,
                isActive = dto.IsActive
            });

            return (bool)results.First()["updated"];
        }

        // ---------------------------------------------------
        // 5. Supprimer un produit
        // ---------------------------------------------------
        public async Task<bool> DeleteProduct(int id)
        {
            var cypher = "MATCH (p:Product {id: $id}) DETACH DELETE p RETURN count(p) > 0 as deleted";
            // Wait, count(p) after delete won't work that way easily if it's already gone.
            // Let's just return true if no exception.
            await _neo4j.RunQueryAsync(cypher, new { id });
            return true;
        }

        // ---------------------------------------------------
        // 6. Récupérer les commandes du prestataire
        // ---------------------------------------------------
        public async Task<List<Order>> GetOrders(string prestataireId)
        {
            var cypher = @"
                MATCH (s:Store {prestataireId: $prestataireId})-[:HAS_ORDER]->(o:Order)
                MATCH (o)-[:ORDERED_PRODUCT]->(p:Product)
                MATCH (o)-[:ORDER_BY]->(u:User)
                RETURN o, p, u
                ORDER BY o.createdAt DESC";
            
            var results = await _neo4j.RunQueryAsync(cypher, new { prestataireId });
            return results.Select(r => {
                var order = MapToOrder(r["o"] as INode);
                order.Product = MapToProduct(r["p"] as INode);
                order.User = MapToUser(r["u"] as INode);
                return order;
            }).ToList();
        }

        public async Task<bool> UpdateOrderStatus(string prestataireId, int orderId, string status)
        {
            var cypher = @"
                MATCH (s:Store {prestataireId: $prestataireId})-[:HAS_ORDER]->(o:Order {id: $orderId})
                SET o.status = $status
                RETURN count(o) > 0 as updated";
            
            var results = await _neo4j.RunQueryAsync(cypher, new { prestataireId, orderId, status });
            return (bool)results.First()["updated"];
        }

        public async Task<List<Product>> GetProducts(string prestataireId)
        {
            var cypher = "MATCH (s:Store {prestataireId: $prestataireId})-[:HAS_PRODUCT]->(p:Product) RETURN p";
            var results = await _neo4j.RunQueryAsync(cypher, new { prestataireId });
            return results.Select(r => MapToProduct(r["p"] as INode)).ToList();
        }

        public async Task<object> GetStats(string prestataireId)
        {
            var cypher = @"
                MATCH (s:Store {prestataireId: $prestataireId})
                OPTIONAL MATCH (s)-[:HAS_PRODUCT]->(p:Product)
                OPTIONAL MATCH (s)-[:HAS_ORDER]->(o:Order)
                OPTIONAL MATCH (p)-[:HAS_REVIEW]->(r:Review)
                RETURN 
                    count(DISTINCT p) as totalProducts,
                    count(DISTINCT o) as totalOrders,
                    count(DISTINCT CASE WHEN o.status = 'pending' THEN o END) as pendingOrders,
                    sum(o.totalPrice) as totalRevenue,
                    count(DISTINCT r) as totalReviews,
                    avg(r.rating) as averageRating";
            
            var results = await _neo4j.RunQueryAsync(cypher, new { prestataireId });
            var res = results.First();
            
            return new
            {
                totalProducts = Convert.ToInt32(res["totalProducts"]),
                totalOrders = Convert.ToInt32(res["totalOrders"]),
                pendingOrders = Convert.ToInt32(res["pendingOrders"]),
                totalReviews = Convert.ToInt32(res["totalReviews"]),
                averageRating = res["averageRating"] != null ? Convert.ToDouble(res["averageRating"]) : 0.0,
                totalRevenue = res["totalRevenue"] != null ? Convert.ToDouble(res["totalRevenue"]) : 0.0
            };
        }

        public async Task<List<Review>> GetReviews(string prestataireId)
        {
            var cypher = @"
                MATCH (s:Store {prestataireId: $prestataireId})-[:HAS_PRODUCT]->(p:Product)<-[:HAS_REVIEW]-(r:Review)
                MATCH (u:User)-[:WROTE_REVIEW]->(r)
                RETURN r, p, u";
            
            var results = await _neo4j.RunQueryAsync(cypher, new { prestataireId });
            return results.Select(res => {
                var review = MapToReview(res["r"] as INode);
                review.Product = MapToProduct(res["p"] as INode);
                review.User = MapToUser(res["u"] as INode);
                return review;
            }).ToList();
        }

        public async Task AddPromotion(string prestataireId, CreatePromotionDto dto)
        {
            var cypher = @"
                MATCH (p:Product {id: $productId})<-[:HAS_PRODUCT]-(s:Store {prestataireId: $prestataireId})
                CREATE (promo:Promotion {
                    id: $id,
                    title: $title,
                    description: $description,
                    discountPercent: $discountPercent,
                    startDate: $startDate,
                    endDate: $endDate,
                    code: $code,
                    usageLimit: $usageLimit,
                    usageCount: 0,
                    productId: $productId
                })
                CREATE (p)-[:HAS_PROMOTION]->(promo)";
            
            await _neo4j.RunQueryAsync(cypher, new 
            {
                prestataireId,
                productId = dto.ProductId,
                id = new Random().Next(1000, 999999),
                title = dto.Title,
                description = dto.Description,
                discountPercent = dto.DiscountPercent,
                startDate = dto.StartDate.ToString("O"),
                endDate = dto.EndDate.ToString("O"),
                code = dto.Code ?? Guid.NewGuid().ToString("N")[..8].ToUpper(),
                usageLimit = dto.UsageLimit
            });
        }

        public async Task<List<Promotion>> GetMyPromotions(string prestataireId)
        {
            var cypher = @"
                MATCH (s:Store {prestataireId: $prestataireId})-[:HAS_PRODUCT]->(p:Product)-[:HAS_PROMOTION]->(promo:Promotion)
                RETURN promo, p
                ORDER BY promo.endDate DESC";
            
            var results = await _neo4j.RunQueryAsync(cypher, new { prestataireId });
            return results.Select(r => {
                var promo = MapToPromotion(r["promo"] as INode);
                promo.Product = MapToProduct(r["p"] as INode);
                return promo;
            }).ToList();
        }

        public async Task<bool> UpdatePromotion(string prestataireId, int id, Promotion dto)
        {
            var cypher = @"
                MATCH (s:Store {prestataireId: $prestataireId})-[:HAS_PRODUCT]->(p:Product)-[:HAS_PROMOTION]->(promo:Promotion {id: $id})
                SET promo.discountPercent = $discountPercent,
                    promo.startDate = $startDate,
                    promo.endDate = $endDate
                RETURN count(promo) > 0 as updated";
            
            var results = await _neo4j.RunQueryAsync(cypher, new { 
                prestataireId,
                id,
                discountPercent = dto.DiscountPercent,
                startDate = dto.StartDate.ToString("O"),
                endDate = dto.EndDate.ToString("O")
            });
            return (bool)results.First()["updated"];
        }

        public async Task<bool> DeletePromotion(string prestataireId, int id)
        {
            var cypher = @"
                MATCH (s:Store {prestataireId: $prestataireId})-[:HAS_PRODUCT]->(p:Product)-[:HAS_PROMOTION]->(promo:Promotion {id: $id})
                DETACH DELETE promo
                RETURN count(promo) > 0 as deleted";
            await _neo4j.RunQueryAsync(cypher, new { prestataireId, id });
            return true;
        }

        // --- Mapping Helpers ---

        private Store MapToStore(INode node)
        {
            return new Store
            {
                Id = Convert.ToInt32(node.Properties["id"]),
                Name = node.Properties["name"].ToString() ?? "",
                Description = node.Properties["description"].ToString() ?? "",
                Address = node.Properties["address"].ToString() ?? "",
                PrestataireId = node.Properties["prestataireId"].ToString() ?? ""
            };
        }

        private Product MapToProduct(INode node)
        {
            return new Product
            {
                Id = Convert.ToInt32(node.Properties["id"]),
                Name = node.Properties["name"].ToString() ?? "",
                Price = Convert.ToDouble(node.Properties["price"]),
                ImageUrl = node.Properties.GetValueOrDefault("imageUrl")?.ToString(),
                StoreId = Convert.ToInt32(node.Properties.GetValueOrDefault("storeId") ?? 0),
                CreatedAt = DateTime.Parse(node.Properties["createdAt"].ToString()),
                IsActive = (bool)(node.Properties.GetValueOrDefault("isActive") ?? true),
                Stock = Convert.ToInt32(node.Properties.GetValueOrDefault("stock") ?? 0),
                Category = node.Properties.GetValueOrDefault("category")?.ToString(),
                Description = node.Properties.GetValueOrDefault("description")?.ToString()
            };
        }

        private Order MapToOrder(INode node)
        {
             return new Order
            {
                Id = Convert.ToInt32(node.Properties["id"]),
                UserId = node.Properties["userId"].ToString() ?? "",
                StoreId = Convert.ToInt32(node.Properties["storeId"]),
                ProductId = Convert.ToInt32(node.Properties["productId"]),
                Quantity = Convert.ToInt32(node.Properties["quantity"]),
                TotalPrice = Convert.ToDouble(node.Properties["totalPrice"]),
                TotalAmount = Convert.ToDouble(node.Properties["totalPrice"]),
                Status = node.Properties["status"].ToString() ?? "Pending",
                CreatedAt = DateTime.Parse(node.Properties["createdAt"].ToString())
            };
        }

        private Promotion MapToPromotion(INode node)
        {
            return new Promotion
            {
                Id = Convert.ToInt32(node.Properties["id"]),
                Title = node.Properties.GetValueOrDefault("title")?.ToString() ?? "",
                Description = node.Properties.GetValueOrDefault("description")?.ToString() ?? "",
                DiscountPercent = Convert.ToInt32(node.Properties["discountPercent"]),
                StartDate = DateTime.Parse(node.Properties["startDate"].ToString()),
                EndDate = DateTime.Parse(node.Properties["endDate"].ToString()),
                Code = node.Properties.GetValueOrDefault("code")?.ToString(),
                UsageLimit = Convert.ToInt32(node.Properties.GetValueOrDefault("usageLimit") ?? 0),
                UsageCount = Convert.ToInt32(node.Properties.GetValueOrDefault("usageCount") ?? 0),
                ProductId = Convert.ToInt32(node.Properties["productId"])
            };
        }

        private Review MapToReview(INode node)
        {
            return new Review
            {
                Id = Convert.ToInt32(node.Properties["id"]),
                UserId = node.Properties["userId"].ToString() ?? "",
                ProductId = Convert.ToInt32(node.Properties["productId"]),
                Rating = Convert.ToInt32(node.Properties["rating"]),
                Comment = node.Properties["comment"].ToString() ?? "",
                CreatedAt = DateTime.Parse(node.Properties["createdAt"].ToString())
            };
        }

        private AppUser MapToUser(INode node)
        {
            return new AppUser
            {
                Id = node.Properties["id"].ToString() ?? "",
                FullName = node.Properties["fullName"].ToString() ?? "",
                Email = node.Properties["email"].ToString() ?? ""
            };
        }
    }
}
