namespace backend.Auth.Dto
{
    public class PrestataireDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool IsApproved { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}