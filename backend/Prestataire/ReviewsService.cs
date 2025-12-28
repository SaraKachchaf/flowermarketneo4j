using backend.Models;
using backend.Prestataire.Dto;
using Neo4j.Driver;

namespace backend.Prestataire
{
    public class ReviewsService
    {
        private readonly Neo4jService _neo4j;

        public ReviewsService(Neo4jService neo4j)
        {
            _neo4j = neo4j;
        }

        // ------------------------------------------------------
        // Créer un avis
        // ------------------------------------------------------
        public async Task<Review?> AddReview(string userId, CreateReviewDto dto)
        {
            var cypherCheck = "MATCH (p:Product {id: $productId}) RETURN p";
            var check = await _neo4j.RunQueryAsync(cypherCheck, new { productId = dto.ProductId });
            if (!check.Any()) return null;

            var cypher = @"
                MATCH (u:User {id: $userId})
                MATCH (p:Product {id: $productId})
                CREATE (r:Review {
                    id: $id,
                    userId: $userId,
                    productId: $productId,
                    rating: $rating,
                    comment: $comment,
                    createdAt: $createdAt
                })
                CREATE (u)-[:WROTE_REVIEW]->(r)
                CREATE (p)<-[:HAS_REVIEW]-(r)
                RETURN r";

            var results = await _neo4j.RunQueryAsync(cypher, new {
                userId,
                productId = dto.ProductId,
                id = new Random().Next(1000, 999999),
                rating = dto.Rating,
                comment = dto.Comment,
                createdAt = DateTime.UtcNow.ToString("O")
            });

            return MapToReview(results.First()["r"] as INode);
        }

        // ------------------------------------------------------
        // Supprimer un avis
        // ------------------------------------------------------
        public async Task<bool> DeleteReview(int reviewId)
        {
            var cypher = "MATCH (r:Review {id: $id}) DETACH DELETE r";
            await _neo4j.RunQueryAsync(cypher, new { id = reviewId });
            return true;
        }

        // ------------------------------------------------------
        // Récupérer les avis d'un produit
        // ------------------------------------------------------
        public async Task<List<Review>> GetReviewsByProductId(int productId)
        {
            var cypher = "MATCH (p:Product {id: $productId})<-[:HAS_REVIEW]-(r:Review) RETURN r";
            var results = await _neo4j.RunQueryAsync(cypher, new { productId });
            return results.Select(res => MapToReview(res["r"] as INode)).ToList();
        }

        // ------------------------------------------------------
        // Modifier un avis
        // ------------------------------------------------------
        public async Task<bool> UpdateReview(int reviewId, UpdateReviewDto dto)
        {
            var cypher = @"
                MATCH (r:Review {id: $id})
                SET r.rating = $rating,
                    r.comment = $comment
                RETURN count(r) > 0 as updated";
            
            var results = await _neo4j.RunQueryAsync(cypher, new {
                id = reviewId,
                rating = dto.Rating,
                comment = dto.Comment
            });

            return (bool)results.First()["updated"];
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
    }
}
