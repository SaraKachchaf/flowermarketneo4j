namespace backend.Admin.Dto
{
    public class AdminStatsDto
    {
        public int PendingPrestataires { get; set; }
        public int TotalClients { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int TotalPrestataires { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingOrders { get; set; }

    }

    public class UserDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? StoreName { get; set; }
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string StoreName { get; set; }
        public string PrestataireName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Stock { get; internal set; }
        public string Category { get; internal set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public decimal TotalPrice { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public DateTime OrderDate { get; set; }
    }
}