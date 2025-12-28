namespace backend.Models
{
    public class Store
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }

        // FK vers AppUser (c'est Store qui dépend d'AppUser)
        public string PrestataireId { get; set; }
        public AppUser Prestataire { get; set; }

        public List<Product> Products { get; set; } = new();
        public List<Order> Orders { get; set; } = new();


    }
}