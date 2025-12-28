using backend.Auth.Dto;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Neo4j.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace backend.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly Neo4jService _neo4j;

        public AuthController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IConfiguration configuration,
            EmailService emailService,
            Neo4jService neo4j)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _emailService = emailService;
            _neo4j = neo4j;
        }

        // =========================
        // INSCRIPTION CLIENT
        // =========================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                if (dto == null) return BadRequest(new { error = "Invalid payload" });

                var userExists = await _userManager.FindByEmailAsync(dto.Email);
                if (userExists != null)
                    return BadRequest(new { message = "Email déjà utilisé" });

                var user = new AppUser
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    UserName = dto.Email,
                    IsApproved = true,
                    EmailConfirmed = true // 🟢 AUTO-CONFIRM
                };

                var result = await _userManager.CreateAsync(user, dto.Password);
                if (!result.Succeeded)
                    return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

                await _userManager.AddToRoleAsync(user, "Client");

                // Pas de code de vérification ni d'email envoyé

                try 
                {
                    // Notification Admin via Neo4j
                    var cypher = @"
                        CREATE (n:Notification {
                            id: $id,
                            title: $title,
                            message: $message,
                            type: $type,
                            isRead: false,
                            createdAt: $createdAt
                        })";
                    
                    await _neo4j.RunQueryAsync(cypher, new {
                        id = Guid.NewGuid().ToString(),
                        title = "Nouveau Client",
                        message = $"Le client {dto.FullName} ({dto.Email}) vient de s'inscrire.",
                        type = "Admin",
                        createdAt = DateTime.UtcNow.ToString("O")
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Notification failed: {ex.Message}");
                }

                return Ok(new
                {
                    message = "Compte créé avec succès.",
                    requiresEmailVerification = false, // 🟢 PLUS DE VERIFICATION
                    email = user.Email
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Erreur serveur", details = ex.Message });
            }
        }

        // =========================
        // INSCRIPTION PRESTATAIRE
        // =========================
        [HttpPost("register-prestataire")]
        public async Task<IActionResult> RegisterPrestataire([FromBody] RegisterPrestataireDto dto)
        {
            try
            {
                if (dto == null) return BadRequest(new { error = "Invalid payload" });

                var userExists = await _userManager.FindByEmailAsync(dto.Email);
                if (userExists != null)
                    return BadRequest(new { error = "Email déjà utilisé" });

                var user = new AppUser
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    UserName = dto.Email,
                    IsApproved = false,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, dto.Password);
                if (!result.Succeeded)
                    return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

                await _userManager.AddToRoleAsync(user, "Prestataire");

                try
                {
                    // Notification Admin via Neo4j
                    var cypher = @"
                        CREATE (n:Notification {
                            id: $id,
                            title: $title,
                            message: $message,
                            type: $type,
                            isRead: false,
                            createdAt: $createdAt
                        })";
                    
                    await _neo4j.RunQueryAsync(cypher, new {
                        id = Guid.NewGuid().ToString(),
                        title = "Nouveau Prestataire",
                        message = $"Le prestataire {dto.FullName} ({dto.Email}) s'est inscrit et est en attente de validation.",
                        type = "Admin",
                        createdAt = DateTime.UtcNow.ToString("O")
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Notification failed: {ex.Message}");
                }

                return Ok(new { message = "Demande envoyée. En attente de validation par l'admin." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Erreur serveur", details = ex.Message });
            }
        }

        // =========================
        // CONNEXION (Keep)
        // =========================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            try
            {
                if (model == null) return BadRequest(new { error = "Invalid payload" });
                if (!ModelState.IsValid) return BadRequest(new { error = "Invalid data" });

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                    return Unauthorized(new { error = "Email ou mot de passe incorrect" });

                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
                if (!result.Succeeded)
                    return Unauthorized(new { error = "Email ou mot de passe incorrect" });

                if (!user.EmailConfirmed)
                    return Unauthorized(new { error = "Veuillez vérifier votre email avant de vous connecter." });

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault();

                if (role == "Prestataire" && !user.IsApproved)
                    return Unauthorized(new { error = "Votre compte prestataire est en attente de validation par l'admin." });

                Console.WriteLine($"[LOGIN SUCCESS] User: {user.Email}, ID for Token: {user.Id}");
                var token = GenerateJwtToken(user, role);

                return Ok(new
                {
                    token,
                    user = new
                    {
                        id = user.Id,
                        fullName = user.FullName,
                        email = user.Email,
                        role,
                        isApproved = user.IsApproved,
                        emailConfirmed = user.EmailConfirmed
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Erreur serveur", details = ex.Message });
            }
        }

        private string GenerateJwtToken(AppUser user, string? role)
        {
            var jwtKey = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new Exception("JWT Key is missing. Check appsettings.json -> Jwt:Key");

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? "")
            };

            if (!string.IsNullOrWhiteSpace(role))
                claims.Add(new Claim(ClaimTypes.Role, role));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(3),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("send-verification")]
        public async Task<IActionResult> SendVerificationEmail([FromBody] SendVerificationRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                    return BadRequest(new { error = "Utilisateur non trouvé" });

                var verificationCode = "123456";
                user.EmailVerificationCode = verificationCode;
                user.EmailVerificationCodeExpiry = DateTime.UtcNow.AddMinutes(15);

                await _userManager.UpdateAsync(user);
                await _emailService.SendVerificationEmailAsync(user.Email!, verificationCode, user.FullName);

                return Ok(new { message = "Email de vérification envoyé", expiresIn = "15 minutes" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Erreur lors de l'envoi", details = ex.Message });
            }
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] EmailVerificationDto dto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                    return BadRequest(new { error = "Utilisateur non trouvé" });

                if (string.IsNullOrEmpty(user.EmailVerificationCode) || user.EmailVerificationCode != dto.Code)
                    return BadRequest(new { error = "Code de vérification incorrect" });

                if (!user.EmailVerificationCodeExpiry.HasValue || user.EmailVerificationCodeExpiry.Value < DateTime.UtcNow)
                    return BadRequest(new { error = "Le code de vérification a expiré" });

                user.EmailConfirmed = true;
                user.EmailVerificationCode = null;
                user.EmailVerificationCodeExpiry = null;

                await _userManager.UpdateAsync(user);

                return Ok(new { success = true, message = "Email vérifié avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Erreur lors de la vérification", details = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized();

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return NotFound(new { error = "Utilisateur non trouvé" });

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault();

                return Ok(new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    email = user.Email,
                    role,
                    isApproved = user.IsApproved,
                    emailConfirmed = user.EmailConfirmed
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Erreur", details = ex.Message });
            }
        }
    }

    public class SendVerificationRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
