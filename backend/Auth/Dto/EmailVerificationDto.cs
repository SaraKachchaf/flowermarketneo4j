// EmailVerificationDto.cs
namespace backend.Auth.Dto
{
    public class EmailVerificationDto
    {
        public string Email { get; set; }
        public string Code { get; set; }
    }
}