using Microsoft.Extensions.Options;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Common.Templates;
using RestaurantSystem.Api.Settings;
using RestaurantSystem.Domain.Entities;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace RestaurantSystem.Api.Common.Services;

/// <summary>
/// Email service implementation using SMTP
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly IWebHostEnvironment _environment;

    public EmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<EmailService> logger,
        IWebHostEnvironment environment)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
        _environment = environment;
    }

    public async Task SendPasswordResetEmailAsync(ApplicationUser user, string resetToken, string? resetUrl = null)
    {
        try
        {
            // Generate reset URL if not provided
            if (string.IsNullOrEmpty(resetUrl))
            {
                resetUrl = $"{_emailSettings.FrontendBaseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(user.Email!)}";
            }

            var subject = EmailTemplates.PasswordReset.Subject;
            var htmlBody = EmailTemplates.PasswordReset.GetHtmlBody(user.FirstName, user.LastName, resetUrl);
            var textBody = EmailTemplates.PasswordReset.GetTextBody(user.FirstName, user.LastName, resetUrl);

            await SendEmailAsync(user.Email!, subject, htmlBody, textBody);

            _logger.LogInformation("Password reset email sent to user {UserId} ({Email})", user.Id, user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to user {UserId} ({Email})", user.Id, user.Email);
            throw;
        }
    }

    public async Task SendWelcomeEmailAsync(ApplicationUser user)
    {
        try
        {
            var subject = EmailTemplates.Welcome.Subject;
            var htmlBody = EmailTemplates.Welcome.GetHtmlBody(user.FirstName, user.LastName, user.Role.ToString());
            var textBody = EmailTemplates.Welcome.GetTextBody(user.FirstName, user.LastName, user.Role.ToString());

            await SendEmailAsync(user.Email!, subject, htmlBody, textBody);

            _logger.LogInformation("Welcome email sent to user {UserId} ({Email})", user.Id, user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to user {UserId} ({Email})", user.Id, user.Email);
            throw;
        }
    }

    public async Task SendEmailVerificationAsync(ApplicationUser user, string verificationToken, string? verificationUrl = null)
    {
        try
        {
            // Generate verification URL if not provided
            if (string.IsNullOrEmpty(verificationUrl))
            {
                verificationUrl = $"{_emailSettings.FrontendBaseUrl}/verify-email?token={Uri.EscapeDataString(verificationToken)}&email={Uri.EscapeDataString(user.Email!)}";
            }

            var subject = EmailTemplates.EmailVerification.Subject;
            var htmlBody = EmailTemplates.EmailVerification.GetHtmlBody(user.FirstName, user.LastName, verificationUrl);
            var textBody = EmailTemplates.EmailVerification.GetTextBody(user.FirstName, user.LastName, verificationUrl);

            await SendEmailAsync(user.Email!, subject, htmlBody, textBody);

            _logger.LogInformation("Email verification sent to user {UserId} ({Email})", user.Id, user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification to user {UserId} ({Email})", user.Id, user.Email);
            throw;
        }
    }

    public async Task SendPasswordChangedNotificationAsync(ApplicationUser user)
    {
        try
        {
            var subject = EmailTemplates.PasswordChanged.Subject;
            var htmlBody = EmailTemplates.PasswordChanged.GetHtmlBody(user.FirstName, user.LastName, DateTime.UtcNow);
            var textBody = EmailTemplates.PasswordChanged.GetTextBody(user.FirstName, user.LastName, DateTime.UtcNow);

            await SendEmailAsync(user.Email!, subject, htmlBody, textBody);

            _logger.LogInformation("Password changed notification sent to user {UserId} ({Email})", user.Id, user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password changed notification to user {UserId} ({Email})", user.Id, user.Email);
            throw;
        }
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, string? textBody = null)
    {
        if (!_emailSettings.EmailsEnabled)
        {
            _logger.LogInformation("Email sending is disabled. Would have sent email to {To} with subject: {Subject}", to, subject);
            return;
        }

        if (_emailSettings.LogEmailsOnly)
        {
            _logger.LogInformation("EMAIL LOG - To: {To}, Subject: {Subject}, Body: {Body}", to, subject, htmlBody);
            return;
        }

        var retryCount = 0;
        var maxRetries = _emailSettings.MaxRetryAttempts;

        while (retryCount <= maxRetries)
        {
            try
            {
                using var client = CreateSmtpClient();
                using var message = CreateMailMessage(to, subject, htmlBody, textBody);

                await client.SendMailAsync(message);

                _logger.LogInformation("Email sent successfully to {To} with subject: {Subject}", to, subject);
                return;
            }
            catch (Exception ex)
            {
                retryCount++;

                if (retryCount > maxRetries)
                {
                    _logger.LogError(ex, "Failed to send email to {To} after {RetryCount} attempts", to, retryCount);
                    throw;
                }

                _logger.LogWarning(ex, "Failed to send email to {To}, attempt {RetryCount}/{MaxRetries}. Retrying...",
                    to, retryCount, maxRetries);

                await Task.Delay(_emailSettings.RetryDelayMs);
            }
        }
    }

    public async Task SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string htmlBody, string? textBody = null)
    {
        var tasks = recipients.Select(recipient => SendEmailAsync(recipient, subject, htmlBody, textBody));

        try
        {
            await Task.WhenAll(tasks);
            _logger.LogInformation("Bulk email sent to {RecipientCount} recipients with subject: {Subject}",
                recipients.Count(), subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send bulk email to some recipients with subject: {Subject}", subject);
            throw;
        }
    }

    public async Task SendReservationConfirmationEmailAsync(string customerEmail, string customerName, string tableNumber,
        DateTime reservationDate, TimeSpan startTime, TimeSpan endTime, int numberOfGuests, string? specialRequests = null)
    {
        try
        {
            var subject = EmailTemplates.ReservationConfirmation.Subject;
            var htmlBody = EmailTemplates.ReservationConfirmation.GetHtmlBody(
                customerName, tableNumber, reservationDate, startTime, endTime, numberOfGuests, specialRequests);
            var textBody = EmailTemplates.ReservationConfirmation.GetTextBody(
                customerName, tableNumber, reservationDate, startTime, endTime, numberOfGuests, specialRequests);

            await SendEmailAsync(customerEmail, subject, htmlBody, textBody);

            _logger.LogInformation("Reservation confirmation email sent to {Email} for table {TableNumber} on {Date}",
                customerEmail, tableNumber, reservationDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reservation confirmation email to {Email}", customerEmail);
            throw;
        }
    }

    public async Task SendReservationApprovedEmailAsync(string customerEmail, string customerName, string tableNumber,
        DateTime reservationDate, TimeSpan startTime, TimeSpan endTime, int numberOfGuests,
        string? specialRequests = null, string? notes = null)
    {
        try
        {
            var subject = EmailTemplates.ReservationApproved.Subject;
            var htmlBody = EmailTemplates.ReservationApproved.GetHtmlBody(
                customerName, tableNumber, reservationDate, startTime, endTime, numberOfGuests, specialRequests, notes);
            var textBody = EmailTemplates.ReservationApproved.GetTextBody(
                customerName, tableNumber, reservationDate, startTime, endTime, numberOfGuests, specialRequests, notes);

            await SendEmailAsync(customerEmail, subject, htmlBody, textBody);

            _logger.LogInformation("Reservation approved email sent to {Email} for table {TableNumber} on {Date}",
                customerEmail, tableNumber, reservationDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send reservation approved email to {Email}", customerEmail);
            throw;
        }
    }

    private SmtpClient CreateSmtpClient()
    {
        var client = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
        {
            EnableSsl = _emailSettings.EnableSsl,
            Timeout = _emailSettings.TimeoutMs
        };

        if (_emailSettings.UseAuthentication)
        {
            client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
        }

        return client;
    }

    private MailMessage CreateMailMessage(string to, string subject, string htmlBody, string? textBody = null)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };

        message.To.Add(new MailAddress(to));

        // Add plain text alternative if provided
        if (!string.IsNullOrEmpty(textBody))
        {
            var plainTextView = AlternateView.CreateAlternateViewFromString(textBody, Encoding.UTF8, "text/plain");
            var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, "text/html");

            message.AlternateViews.Add(plainTextView);
            message.AlternateViews.Add(htmlView);
        }

        // Set email priority to normal
        message.Priority = MailPriority.Normal;

        return message;
    }

    public async Task SendOrderConfirmationEmailAsync(string customerEmail, string customerName, string orderNumber,
        string orderType, decimal total, IEnumerable<(string name, int quantity, decimal price)> items,
        string? specialInstructions = null, string? deliveryAddress = null)
    {
        try
        {
            var subject = EmailTemplates.OrderConfirmation.Subject;
            var htmlBody = EmailTemplates.OrderConfirmation.GetHtmlBody(
                customerName, orderNumber, orderType, total, items, specialInstructions, deliveryAddress);
            var textBody = EmailTemplates.OrderConfirmation.GetTextBody(
                customerName, orderNumber, orderType, total, items, specialInstructions, deliveryAddress);

            await SendEmailAsync(customerEmail, subject, htmlBody, textBody);

            _logger.LogInformation("Order confirmation email sent to {Email} for order {OrderNumber}",
                customerEmail, orderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order confirmation email to {Email} for order {OrderNumber}",
                customerEmail, orderNumber);
            throw;
        }
    }

    public async Task SendOrderConfirmationAdminEmailAsync(string adminEmail, string orderNumber, string customerName,
        string customerEmail, string customerPhone, string orderType, decimal total,
        IEnumerable<(string name, int quantity, decimal price)> items, string? specialInstructions = null,
        string? deliveryAddress = null)
    {
        try
        {
            var subject = EmailTemplates.OrderConfirmationAdmin.Subject;
            var htmlBody = EmailTemplates.OrderConfirmationAdmin.GetHtmlBody(
                orderNumber, customerName, customerEmail, customerPhone, orderType, total, items,
                specialInstructions, deliveryAddress);
            var textBody = EmailTemplates.OrderConfirmationAdmin.GetTextBody(
                orderNumber, customerName, customerEmail, customerPhone, orderType, total, items,
                specialInstructions, deliveryAddress);

            await SendEmailAsync(adminEmail, subject, htmlBody, textBody);

            _logger.LogInformation("Order notification email sent to admin {Email} for order {OrderNumber}",
                adminEmail, orderNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order notification email to admin {Email} for order {OrderNumber}",
                adminEmail, orderNumber);
            throw;
        }
    }
}