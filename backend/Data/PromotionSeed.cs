using backend.Services;

namespace backend.Data
{
    public static class PromotionSeed
    {
        public static async Task SeedPromotions(Neo4jService neo4j)
        {
            // Vérifier si des promotions existent déjà
            var checkPromo = "MATCH (p:Promotion) RETURN count(p) as count";
            var promoResults = await neo4j.RunQueryAsync(checkPromo);
            if (Convert.ToInt32(promoResults.First()["count"]) > 0)
            {
                return;
            }

            // Récupérer quelques produits actifs
            var getProducts = "MATCH (p:Product {isActive: true}) RETURN p LIMIT 3";
            var productResults = await neo4j.RunQueryAsync(getProducts);

            if (!productResults.Any())
            {
                return;
            }

            var products = productResults.Select(r => (Neo4j.Driver.INode)r["p"]).ToList();
            var now = DateTime.UtcNow;

            for (int i = 0; i < products.Count; i++)
            {
                var pId = Convert.ToInt32(products[i].Properties["id"]);
                var promoId = new Random().Next(1000, 999999);
                
                string title = i switch {
                    0 => "Offre de Bienvenue",
                    1 => "Promotion d'Hiver",
                    _ => "Vente Flash"
                };

                string code = i switch {
                    0 => "WELCOME20",
                    1 => "WINTER15",
                    _ => "FLASH30"
                };

                int discount = i switch {
                    0 => 20,
                    1 => 15,
                    _ => 30
                };

                var cypher = @"
                    MATCH (p:Product {id: $productId})
                    CREATE (promo:Promotion {
                        id: $id,
                        title: $title,
                        description: $description,
                        discountPercent: $discountPercent,
                        startDate: $startDate,
                        endDate: $endDate,
                        code: $code,
                        usageLimit: 100,
                        usageCount: 0,
                        productId: $productId
                    })
                    CREATE (p)-[:HAS_PROMOTION]->(promo)";
                
                await neo4j.RunQueryAsync(cypher, new {
                    productId = pId,
                    id = promoId,
                    title = title,
                    description = "Description de la promotion",
                    discountPercent = discount,
                    startDate = now.AddDays(-1).ToString("O"),
                    endDate = now.AddDays(7).ToString("O"),
                    code = code
                });
            }
        }
    }
}
