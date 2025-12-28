namespace backend.Models
{
    public class Promotion
    {
        public int Id { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }
        public double DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public string Code { get; set; } = string.Empty;
        public int UsageCount { get; set; } = 0;
        public int? UsageLimit { get; set; } // null = illimité
    }
}