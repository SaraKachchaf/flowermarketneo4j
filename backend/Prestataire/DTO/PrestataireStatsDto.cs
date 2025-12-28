namespace backend.Prestataire.DTO
{
    namespace backend.Prestataire.DTO
    {
        public class PrestataireStatsDto
        {
            public int TotalProducts { get; set; }
            public int TotalOrders { get; set; }
            public int PendingOrders { get; set; }
            public int TotalReviews { get; set; }
            public double AverageRating { get; set; }
            public double TotalRevenue { get; set; }
        }
    }
}
