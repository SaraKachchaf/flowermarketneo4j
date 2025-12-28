namespace backend.Prestataire.Dto
{
    public class CreateProductDto
    {
        public string Name { get; set; }
        public double Price { get; set; }
        public string ImageUrl { get; set; }
        public int Stock { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
    }
}