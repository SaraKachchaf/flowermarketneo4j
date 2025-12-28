using backend.Auth.Dto;
using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
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
        private readonly AuthService _authService;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EmailService _emailService;
        private readonly FlowerMarketDbContext _context;

        public AuthController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IConfiguration configuration,
            AuthService authService,
            RoleManager<IdentityRole> roleManager,
            EmailService emailService,
            FlowerMarketDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _authService = authService;
            _roleManager = roleManager;
            _emailService = emailService;
            _context = context;
        }

        // =========================
        // INSCRIPTION CLIENT
        // =========================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var userExists = await _userManager.FindByEmailAsync(dto.Email);
                if (userExists != null)
                {
                    return BadRequest(new { error = "Email déjà utilisé" });
                }

                var user = new AppUser
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    UserName = dto.Email,
                    IsApproved = true, // Client automatiquement approuvé
                    EmailConfirmed = false // Email non vérifié initialement
                };

                var result = await _userManager.CreateAsync(user, dto.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
                }

                await _userManager.AddToRoleAsync(user, "Client");

                // Envoyer l'email de vérification
                var verificationCode = new Random().Next(100000, 999999).ToString();
                user.EmailVerificationCode = verificationCode;
                user.EmailVerificationCodeExpiry = DateTime.Now.AddMinutes(15);

                await _userManager.UpdateAsync(user);
                await _emailService.SendVerificationEmailAsync(
                    user.Email,
                    verificationCode,
                    user.FullName
                );

                // Notification pour l'admin (nouveau client)
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = "Nouveau Client",
                    Message = $"Le client {dto.FullName} ({dto.Email}) vient de s'inscrire.",
                    Type = "Admin",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(notification);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Compte créé avec succès. Veuillez vérifier votre email.",
                    requiresEmailVerification = true,
                    email = user.Email
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Erreur serveur",
                    details = ex.Message
                });
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
                var userExists = await _userManager.FindByEmailAsync(dto.Email);
                if (userExists != null)
                {
                    return BadRequest(new { error = "Email déjà utilisé" });
                }

                var user = new AppUser
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    UserName = dto.Email,
                    IsApproved = false // En attente d'approbation admin
                };

                var result = await _userManager.CreateAsync(user, dto.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
                }

                await _userManager.AddToRoleAsync(user, "Prestataire");

                // Notification pour l'admin
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = "Nouveau Prestataire",
                    Message = $"Le prestataire {dto.FullName} ({dto.Email}) s'est inscrit et est en attente de validation.",
                    Type = "Admin",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Notifications.AddAsync(notification);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Demande envoyée. En attente de validation par l'admin."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Erreur serveur",
                    details = ex.Message
                });
            }
        }

        // =========================
        // CONNEXION
        // =========================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                {
                    return Unauthorized(new { error = "Email ou mot de passe incorrect" });
                }

                // Vérifier si l'email est confirmé (uniquement pour les clients)
                var roles = await _userManager.GetRolesAsync(user);
                bool isClient = roles.Contains("Client");

                if (isClient && !user.EmailConfirmed)
                {
                    // Envoyer un nouveau code de vérification
                    var verificationCode = new Random().Next(100000, 999999).ToString();
                    user.EmailVerificationCode = verificationCode;
                    user.EmailVerificationCodeExpiry = DateTime.Now.AddMinutes(15);

                    await _userManager.UpdateAsync(user);
                    await _emailService.SendVerificationEmailAsync(
                        user.Email,
                        verificationCode,
                        user.FullName
                    );

                    return BadRequest(new
                    {
                        requiresEmailVerification = true,
                        message = "Veuillez vérifier votre email avant de vous connecter. Un nouveau code a été envoyé.",
                        email = user.Email
                    });
                }

                // Vérifier le mot de passe
                var result = await _signInManager.CheckPasswordSignInAsync(
                    user,
                    dto.Password,
                    false
                );

                if (!result.Succeeded)
                {
                    return Unauthorized(new { error = "Email ou mot de passe incorrect" });
                }

                var role = roles.FirstOrDefault();

                if (string.IsNullOrEmpty(role))
                {
                    return StatusCode(500, new { error = "L'utilisateur n'a aucun rôle assigné" });
                }

                // Vérifier si prestataire est approuvé
                if (role == "Prestataire" && !user.IsApproved)
                {
                    return Unauthorized(new
                    {
                        message = "PENDING_APPROVAL",
                        isApproved = false
                    });
                }

                // Génération du token JWT
                var token = _authService.GenerateToken(user, roles);

                return Ok(new
                {
                    token = token,
                    role = role,
                    fullName = user.FullName,
                    isApproved = user.IsApproved,
                    emailConfirmed = user.EmailConfirmed
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Erreur serveur lors de la connexion",
                    details = ex.Message
                });
            }
        }

        // =========================
        // VERIFICATION EMAIL (2FA)
        // =========================
        [HttpPost("send-verification")]
        public async Task<IActionResult> SendVerificationEmail([FromBody] SendVerificationRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                    return BadRequest(new { error = "Utilisateur non trouvé" });

                // Générer un code de 6 chiffres
                var verificationCode = new Random().Next(100000, 999999).ToString();

                // Stocker le code avec une expiration (15 minutes)
                user.EmailVerificationCode = verificationCode;
                user.EmailVerificationCodeExpiry = DateTime.Now.AddMinutes(15);

                await _userManager.UpdateAsync(user);

                // Envoyer l'email
                await _emailService.SendVerificationEmailAsync(
                    user.Email,
                    verificationCode,
                    user.FullName
                );

                return Ok(new
                {
                    message = "Email de vérification envoyé",
                    expiresIn = "15 minutes"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Erreur lors de l'envoi de l'email de vérification",
                    details = ex.Message
                });
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

                // Vérifier si le code correspond et n'est pas expiré
                if (string.IsNullOrEmpty(user.EmailVerificationCode) || user.EmailVerificationCode != dto.Code)
                    return BadRequest(new { error = "Code de vérification incorrect" });

                if (user.EmailVerificationCodeExpiry < DateTime.Now)
                    return BadRequest(new { error = "Le code de vérification a expiré" });

                // Marquer l'email comme vérifié
                user.EmailConfirmed = true;
                user.EmailVerificationCode = null;
                user.EmailVerificationCodeExpiry = null;

                await _userManager.UpdateAsync(user);

                return Ok(new
                {
                    success = true,
                    message = "Email vérifié avec succès",
                    user = new
                    {
                        user.Email,
                        user.FullName,
                        EmailConfirmed = user.EmailConfirmed
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Erreur lors de la vérification de l'email",
                    details = ex.Message
                });
            }
        }

        // =========================
        // GESTION PRESTATAIRES (ADMIN)
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpGet("pending-prestataires")]
        public async Task<IActionResult> GetPendingPrestataires()
        {
            try
            {
                // Récupérer tous les prestataires
                var allUsers = await _userManager.GetUsersInRoleAsync("Prestataire");
                var pendingPrestataires = allUsers.Where(u => !u.IsApproved).ToList();

                if (!pendingPrestataires.Any())
                {
                    return Ok(new
                    {
                        message = "Aucun prestataire en attente d'approbation.",
                        totalPrestataires = allUsers.Count
                    });
                }

                var result = pendingPrestataires.Select(u => new PrestataireDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    IsApproved = u.IsApproved,
                    
                }).ToList();

                return Ok(new
                {
                    count = result.Count,
                    prestataires = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("approve-prestataire")]
        public async Task<IActionResult> ApprovePrestataire([FromBody] ApprovePrestataireDto dto)
        {
            try
            {
                // Trouver l'utilisateur
                var user = await _userManager.FindByIdAsync(dto.UserId);
                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Utilisateur introuvable."
                    });
                }

                // Vérifier que c'est bien un prestataire
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Prestataire"))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Cet utilisateur n'est pas un prestataire."
                    });
                }

                // Mettre à jour le statut d'approbation
                user.IsApproved = dto.IsApproved;
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    // Notification pour le prestataire
                    var statusMessage = dto.IsApproved
                        ? "Votre compte prestataire a été approuvé. Vous pouvez maintenant vous connecter."
                        : "Votre compte prestataire a été désactivé par l'administrateur.";

                    var notification = new Notification
                    {
                        Id = Guid.NewGuid(),
                        Title = dto.IsApproved ? "Compte Approuvé" : "Compte Désactivé",
                        Message = statusMessage,
                        Type = "Prestataire",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _context.Notifications.AddAsync(notification);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        success = true,
                        message = dto.IsApproved
                            ? "Prestataire approuvé avec succès !"
                            : "Prestataire désactivé avec succès !",
                        approvedAt = DateTime.Now,
                        user = new
                        {
                            user.Id,
                            user.FullName,
                            user.Email,
                            user.IsApproved
                        }
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = "Erreur lors de la mise à jour du statut",
                    errors = result.Errors.Select(e => e.Description)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erreur interne",
                    error = ex.Message
                });
            }
        }

        // =========================
        // GET CURRENT USER INFO
        // =========================
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { error = "Utilisateur non trouvé" });

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault();

                return Ok(new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    email = user.Email,
                    role = role,
                    isApproved = user.IsApproved,
                    emailConfirmed = user.EmailConfirmed,
                   
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Erreur lors de la récupération des informations utilisateur",
                    details = ex.Message
                });
            }
        }
    }

    // Classe pour la requête d'envoi de vérification
    public class SendVerificationRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}