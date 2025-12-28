namespace backend.Models
{
    public class Notification
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // Admin / Client / Prestataire
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; }
    }

}
