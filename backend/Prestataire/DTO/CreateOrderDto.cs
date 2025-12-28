namespace backend.Prestataire.Dto
{
    public class CreateOrderDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public string CustomerId { get; set; }  // ou un autre identifiant pour le client
        public string Status { get; set; } // Ex: "Pending", "Completed"
    }
}