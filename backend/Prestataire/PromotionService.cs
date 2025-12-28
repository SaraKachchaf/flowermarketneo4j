using backend.Models;
using backend.Prestataire.Dto;
using Neo4j.Driver;

namespace backend.Prestataire
{
    public class PromotionService
    {
        private readonly Neo4jService _neo4j;

        public PromotionService(Neo4jService neo4j)
        {
            _neo4j = neo4j;
        }

        // -----------------------------------------------------
        // Récupérer toutes les promotions du prestataire
        // -----------------------------------------------------
        public async Task<List<Promotion>> GetMyPromotions(string prestataireId)
        {
            var cypher = @"
                MATCH (s:Store {prestataireId: $prestataireId})-[:HAS_PRODUCT]->(p:Product)-[:HAS_PROMOTION]->(promo:Promotion)
                RETURN promo, p";
            
            var results = await _neo4j.RunQueryAsync(cypher, new { prestataireId });
            return results.Select(r => {
                var promo = MapToPromotion(r["promo"] as INode);
                promo.Product = MapToProduct(r["p"] as INode);
                return promo;
            }).ToList();
        }

        // -----------------------------------------------------
        // Ajouter une promotion à un produit du prestataire
        // -----------------------------------------------------
        public async Task<Promotion?> AddPromotion(string prestataireId, CreatePromotionDto dto)
        {
            var cypherCheck = @"
                MATCH (s:Store {prestataireId: $prestataireId})-[:HAS_PRODUCT]->(p:Product {id: $productId})
                RETURN p";
            
            var check = await _neo4j.RunQueryAsync(cypherCheck, new { prestataireId, productId = dto.ProductId });
            if (!check.Any()) return null;

            var cypher = @"
                MATCH (p:Product {id: $productId})
                CREATE (promo:Promotion {
                    id: $id,
                    productId: $productId,
                    title: $title,
                    description: $description,
                    discountPercent: $discountPercent,
                    startDate: $startDate,
                    endDate: $endDate
                })
                CREATE (p)-[:HAS_PROMOTION]->(promo)
                RETURN promo";

            var results = await _neo4j.RunQueryAsync(cypher, new {
                productId = dto.ProductId,
                id = new Random().Next(1000, 999999),
                title = dto.Title,
                description = dto.Description,
                discountPercent = dto.DiscountPercent,
                startDate = dto.StartDate.ToString("O"),
                endDate = dto.EndDate.ToString("O")
            });

            return MapToPromotion(results.First()["promo"] as INode);
        }

        // -----------------------------------------------------
        // Modifier une promotion
        // -----------------------------------------------------
        public async Task<bool> UpdatePromotion(int id, UpdatePromotionDto dto)
        {
            var cypher = @"
                MATCH (promo:Promotion {id: $id})
                SET promo.title = $title,
                    promo.description = $description,
                    promo.discountPercent = $discountPercent,
                    promo.startDate = $startDate,
                    promo.endDate = $endDate
                RETURN count(promo) > 0 as updated";
            
            var results = await _neo4j.RunQueryAsync(cypher, new {
                id,
                title = dto.Title,
                description = dto.Description,
                discountPercent = dto.DiscountPercent,
                startDate = dto.StartDate.ToString("O"),
                endDate = dto.EndDate.ToString("O")
            });

            return (bool)results.First()["updated"];
        }

        // -----------------------------------------------------
        // Supprimer une promotion
        // -----------------------------------------------------
        public async Task<bool> DeletePromotion(int id)
        {
            var cypher = "MATCH (promo:Promotion {id: $id}) DETACH DELETE promo";
            await _neo4j.RunQueryAsync(cypher, new { id });
            return true;
        }

        private Promotion MapToPromotion(INode node)
        {
            return new Promotion
            {
                Id = Convert.ToInt32(node.Properties["id"]),
                ProductId = Convert.ToInt32(node.Properties["productId"]),
                Title = node.Properties["title"].ToString() ?? "",
                Description = node.Properties["description"].ToString() ?? "",
                DiscountPercent = Convert.ToDouble(node.Properties["discountPercent"]),
                StartDate = DateTime.Parse(node.Properties["startDate"].ToString()),
                EndDate = DateTime.Parse(node.Properties["endDate"].ToString())
            };
        }

        private Product MapToProduct(INode node)
        {
            return new Product
            {
                Id = Convert.ToInt32(node.Properties["id"]),
                Name = node.Properties["name"].ToString() ?? "",
                Price = Convert.ToDouble(node.Properties["price"])
            };
        }
    }
}
