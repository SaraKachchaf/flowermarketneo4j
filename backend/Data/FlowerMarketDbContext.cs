using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public class FlowerMarketDbContext : IdentityDbContext<AppUser, IdentityRole, string>
    {
        public FlowerMarketDbContext(DbContextOptions<FlowerMarketDbContext> options)
            : base(options)
        {
        }

        // Définir vos autres DbSet pour les entités personnalisées
        public DbSet<Store> Stores { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Notification> Notifications { get; set; }


        // On peut personnaliser les noms de table d'Identity si nécessaire
       protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<CartItem>()
        .HasOne(c => c.Product)
        .WithMany()
        .HasForeignKey(c => c.ProductId)
        .OnDelete(DeleteBehavior.NoAction);

    modelBuilder.Entity<CartItem>()
        .HasOne(c => c.User)
        .WithMany()
        .HasForeignKey(c => c.UserId)
        .OnDelete(DeleteBehavior.NoAction);
}

    }


}
