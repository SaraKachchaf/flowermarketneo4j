namespace backend.Models
{
    public class Order
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int StoreId { get; set; }
        public Store Store { get; set; }

        public string UserId { get; set; }
        public AppUser User { get; set; }

        public DateTime CreatedAt { get; set; }
        public int Quantity { get; set; }
        public double TotalPrice { get; set; }
        public double TotalAmount { get => TotalPrice; set => TotalPrice = value; } // For frontend compatibility
        public string Status { get; set; } = "pending"; // pending/confirmed/processing/shipped/delivered/cancelled

    }
}