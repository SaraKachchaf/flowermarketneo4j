using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public static class PromotionSeed
    {
        public static async Task SeedPromotions(FlowerMarketDbContext context)
        {
            // Vérifier si des promotions existent déjà
            if (await context.Promotions.AnyAsync())
            {
                return;
            }

            // Récupérer quelques produits actifs
            var products = await context.Products.Where(p => p.IsActive).Take(3).ToListAsync();

            if (products.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var promotions = new List<Promotion>();

            if (products.Count >= 1)
            {
                promotions.Add(new Promotion
                {
                    ProductId = products[0].Id,
                    Title = "Offre de Bienvenue",
                    Description = "Profitez de 20% de réduction sur notre sélection de roses.",
                    DiscountPercent = 20,
                    StartDate = now.AddDays(-1),
                    EndDate = now.AddDays(7),
                    Code = "WELCOME20"
                });
            }

            if (products.Count >= 2)
            {
                promotions.Add(new Promotion
                {
                    ProductId = products[1].Id,
                    Title = "Promotion d'Hiver",
                    Description = "Les lys sont à l'honneur ce mois-ci !",
                    DiscountPercent = 15,
                    StartDate = now.AddDays(-2),
                    EndDate = now.AddDays(14),
                    Code = "WINTER15"
                });
            }

            if (products.Count >= 3)
            {
                promotions.Add(new Promotion
                {
                    ProductId = products[2].Id,
                    Title = "Vente Flash",
                    Description = "Dernière chance pour nos compositions artisanales.",
                    DiscountPercent = 30,
                    StartDate = now.AddDays(-1),
                    EndDate = now.AddDays(1),
                    Code = "FLASH30"
                });
            }

            context.Promotions.AddRange(promotions);
            await context.SaveChangesAsync();
        }
    }
}
