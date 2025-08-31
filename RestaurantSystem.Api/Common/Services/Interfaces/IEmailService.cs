using RestaurantSystem.Domain.Entities;

namespace RestaurantSystem.Api.Common.Services.Interfaces;

/// <summary>
/// Interface for email service operations
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a password reset email to the user
    /// </summary>
    /// <param name="user">The user requesting password reset</param>
    /// <param name="resetToken">The password reset token</param>
    /// <param name="resetUrl">The complete reset URL (optional, will be generated if not provided)</param>
    /// <returns>Task representing the async operation</returns>
    Task SendPasswordResetEmailAsync(ApplicationUser user, string resetToken, string? resetUrl = null);

    /// <summary>
    /// Sends a welcome email to newly registered users
    /// </summary>
    /// <param name="user">The newly registered user</param>
    /// <returns>Task representing the async operation</returns>
    Task SendWelcomeEmailAsync(ApplicationUser user);

    /// <summary>
    /// Sends an email verification email
    /// </summary>
    /// <param name="user">The user to verify</param>
    /// <param name="verificationToken">Email verification token</param>
    /// <param name="verificationUrl">The complete verification URL (optional)</param>
    /// <returns>Task representing the async operation</returns>
    Task SendEmailVerificationAsync(ApplicationUser user, string verificationToken, string? verificationUrl = null);

    /// <summary>
    /// Sends a password changed notification email
    /// </summary>
    /// <param name="user">The user whose password was changed</param>
    /// <returns>Task representing the async operation</returns>
    Task SendPasswordChangedNotificationAsync(ApplicationUser user);

    /// <summary>
    /// Sends a generic email
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlBody">HTML body content</param>
    /// <param name="textBody">Plain text body content (optional)</param>
    /// <returns>Task representing the async operation</returns>
    Task SendEmailAsync(string to, string subject, string htmlBody, string? textBody = null);

    /// <summary>
    /// Sends an email to multiple recipients
    /// </summary>
    /// <param name="recipients">List of recipient email addresses</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlBody">HTML body content</param>
    /// <param name="textBody">Plain text body content (optional)</param>
    /// <returns>Task representing the async operation</returns>
    Task SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string htmlBody, string? textBody = null);
}
