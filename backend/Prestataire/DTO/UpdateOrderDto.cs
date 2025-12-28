namespace backend.Prestataire.Dto
{
    public class UpdateOrderDto
    {
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } // Ex: "Pending", "Completed", "Shipped"
    }
}