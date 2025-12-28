namespace backend.Prestataire.Dto
{
    public class UpdatePromotionDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public double DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}