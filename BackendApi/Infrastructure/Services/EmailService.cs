using BackendApi.Application.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace BackendApi.Infrastructure.Services;

/// <summary>
/// Email service implementation
/// Currently uses SMTP, can be replaced with SendGrid, AWS SES, etc.
/// </summary>
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendVerificationEmailAsync(string to, string verificationToken, string userName)
    {
        var verificationUrl = $"{_configuration["AppSettings:BaseUrl"]}/account/verify?token={verificationToken}";
        var subject = "E-posta Doğrulama";
        var body = $@"
            <html>
            <body>
                <h2>Merhaba {userName},</h2>
                <p>Hesabınızı doğrulamak için aşağıdaki linke tıklayın:</p>
                <p><a href='{verificationUrl}'>E-postamı Doğrula</a></p>
                <p>Veya bu linki tarayıcınıza yapıştırın: {verificationUrl}</p>
                <p>Bu link 24 saat geçerlidir.</p>
            </body>
            </html>";

        await SendNotificationEmailAsync(to, subject, body, isHtml: true);
        _logger.LogInformation("Verification email sent to {Email}", to);
    }

    public async Task SendPasswordResetEmailAsync(string to, string resetToken, string userName)
    {
        var resetUrl = $"{_configuration["AppSettings:BaseUrl"]}/account/reset-password?token={resetToken}";
        var subject = "Şifre Sıfırlama";
        var body = $@"
            <html>
            <body>
                <h2>Merhaba {userName},</h2>
                <p>Şifrenizi sıfırlamak için aşağıdaki linke tıklayın:</p>
                <p><a href='{resetUrl}'>Şifremi Sıfırla</a></p>
                <p>Veya bu linki tarayıcınıza yapıştırın: {resetUrl}</p>
                <p>Bu link 1 saat geçerlidir.</p>
                <p>Eğer bu işlemi siz yapmadıysanız, bu e-postayı görmezden gelebilirsiniz.</p>
            </body>
            </html>";

        await SendNotificationEmailAsync(to, subject, body, isHtml: true);
        _logger.LogInformation("Password reset email sent to {Email}", to);
    }

    public async Task SendRecipeCreatedNotificationAsync(int recipeId, string recipeTitle, string? userEmail)
    {
        var adminEmail = _configuration["AppSettings:AdminEmail"] ?? "admin@example.com";
        var subject = "Yeni Tarif Eklendi";
        var body = $@"
            <html>
            <body>
                <h2>Yeni Tarif Bildirimi</h2>
                <p><strong>Tarif:</strong> {recipeTitle}</p>
                <p><strong>Tarif ID:</strong> {recipeId}</p>
                <p><strong>Ekleyen Kullanıcı:</strong> {userEmail ?? "Bilinmiyor"}</p>
                <p><a href='{_configuration["AppSettings:BaseUrl"]}/admin/recipes/{recipeId}'>Tarifi Görüntüle</a></p>
            </body>
            </html>";

        await SendNotificationEmailAsync(adminEmail, subject, body, isHtml: true);
        _logger.LogInformation("Recipe created notification sent to admin for recipe {RecipeId}", recipeId);
    }

    public async Task SendNotificationEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp-mail.outlook.com";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["Email:Username"] ?? throw new InvalidOperationException("Email:Username is not configured");
            var smtpPassword = _configuration["Email:Password"] ?? throw new InvalidOperationException("Email:Password is not configured");
            var fromEmail = _configuration["Email:From"] ?? smtpUsername;
            var fromName = _configuration["Email:FromName"] ?? "Recipe Site";

            // Create email message
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isHtml)
            {
                bodyBuilder.HtmlBody = body;
            }
            else
            {
                bodyBuilder.TextBody = body;
            }
            message.Body = bodyBuilder.ToMessageBody();

            // Send email using SMTP
            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUsername, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {To} with subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {To}", to);
            throw;
        }
    }
}

