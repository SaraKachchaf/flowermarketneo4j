
using MimeKit;
using MailKit.Security;
using MailKitSmtpClient = MailKit.Net.Smtp.SmtpClient; // Alias

namespace backend.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendVerificationEmailAsync(string email, string verificationCode, string fullName)
        {
            try
            {
                var senderName = _config["EmailSettings:SenderName"];
                var senderEmail = _config["EmailSettings:SenderEmail"];
                var server = _config["EmailSettings:Server"];

                if (string.IsNullOrEmpty(senderName) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(server))
                {
                    LogToConsole(email, verificationCode);
                    return;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress(fullName, email));
                message.Subject = "Confirmez votre email - FlowerMarket";

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 10px;'>
                        <h2 style='color: #4CAF50; text-align: center;'>FlowerMarket</h2>
                        <h3 style='color: #333;'>Confirmez votre email</h3>
                        <p>Bonjour {fullName},</p>
                        <p>Merci de vous être inscrit sur FlowerMarket. Pour activer votre compte, veuillez utiliser le code de vérification suivant :</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <div style='font-size: 32px; font-weight: bold; letter-spacing: 10px; color: #4CAF50; background-color: #f5f5f5; padding: 15px; border-radius: 8px; display: inline-block;'>
                                {verificationCode}
                            </div>
                        </div>
                        <p style='color: #666; font-size: 14px;'>Ce code expirera dans 15 minutes.</p>
                    </div>";

                bodyBuilder.TextBody = $"Bonjour {fullName},\n\nCode de vérification : {verificationCode}";
                message.Body = bodyBuilder.ToMessageBody();

                using var smtp = new MailKitSmtpClient();
                await smtp.ConnectAsync(server, int.Parse(_config["EmailSettings:Port"] ?? "587"), SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_config["EmailSettings:Username"], _config["EmailSettings:Password"]);
                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation($"Email envoyé à {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur d'envoi d'email : {ex.Message}. Simulation locale.");
                LogToConsole(email, verificationCode);
            }
        }

        private void LogToConsole(string email, string code)
        {
            _logger.LogWarning("Email simulation fallback.");
            _logger.LogInformation($"--- EMAIL SIMULATION ---");
            _logger.LogInformation($"To: {email}");
            _logger.LogInformation($"Code: {code}");
            _logger.LogInformation($"------------------------");
        }
    }
}