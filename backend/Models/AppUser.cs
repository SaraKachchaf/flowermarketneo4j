using Microsoft.AspNetCore.Identity;

namespace backend.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public bool IsApproved { get; set; }= false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public bool EmailConfirmed { get; set; } = false; // Nouveau
        public string? EmailVerificationCode { get; set; } // Nouveau
        public DateTime? EmailVerificationCodeExpiry { get; set; } // Nouveau




        // IdentityUser a déjà: Id, UserName, Email, etc.
    }
}
