namespace backend.Prestataire.Dto
{
    public class CreatePromotionDto
    {
        public int ProductId { get; set; }              // ❗ Obligatoire, pas nullable
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double DiscountPercent { get; set; }     // ❗ % de réduction
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Code { get; set; } = string.Empty;
        public int? UsageLimit { get; set; }

    }
}