namespace backend.Models
{
    public class Product
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public double Price { get; set; }
        public string? ImageUrl { get; set; } // Make nullable

        public int StoreId { get; set; }
        public Store? Store { get; set; }
        // Liste des promotions
        public List<Promotion> Promotions { get; set; } = new List<Promotion>();
        public int Stock { get; set; }
        public string? Category { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<Order> Orders { get; set; } = new();
        public List<Review> Reviews { get; set; } = new();
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;



    }
}
