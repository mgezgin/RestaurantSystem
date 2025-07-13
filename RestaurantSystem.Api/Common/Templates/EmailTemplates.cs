namespace RestaurantSystem.Api.Common.Templates;

/// <summary>
/// Static class containing email templates
/// </summary>
public static class EmailTemplates
{
    /// <summary>
    /// Password reset email template
    /// </summary>
    public static class PasswordReset
    {
        public static string Subject => "Reset Your Password - Restaurant System";

        public static string GetHtmlBody(string firstName, string lastName, string resetUrl, int expirationMinutes = 60)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Reset</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #2c3e50; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px 20px; background: #f9f9f9; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #3498db; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
        .warning {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🍽️ Restaurant System</h1>
        </div>
        <div class='content'>
            <h2>Password Reset Request</h2>
            <p>Hello {firstName} {lastName},</p>
            <p>We received a request to reset your password for your Restaurant System account. If you didn't make this request, please ignore this email.</p>
            <p>To reset your password, click the button below:</p>
            <div style='text-align: center;'>
                <a href='{resetUrl}' class='button'>Reset Password</a>
            </div>
            <p>Or copy and paste this link into your browser:</p>
            <p style='word-break: break-all;'>{resetUrl}</p>
            <div class='warning'>
                <strong>⚠️ Important:</strong> This link will expire in {expirationMinutes} minutes for security reasons.
            </div>
            <p>If you have any questions, please contact our support team.</p>
            <p>Best regards,<br>The Restaurant System Team</p>
        </div>
        <div class='footer'>
            <p>This is an automated message, please do not reply to this email.</p>
            <p>© 2024 Restaurant System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        public static string GetTextBody(string firstName, string lastName, string resetUrl, int expirationMinutes = 60)
        {
            return $@"Restaurant System - Password Reset Request

Hello {firstName} {lastName},

We received a request to reset your password for your Restaurant System account. If you didn't make this request, please ignore this email.

To reset your password, visit the following link:
{resetUrl}

IMPORTANT: This link will expire in {expirationMinutes} minutes for security reasons.

If you have any questions, please contact our support team.

Best regards,
The Restaurant System Team

This is an automated message, please do not reply to this email.
© 2024 Restaurant System. All rights reserved.";
        }
    }

    /// <summary>
    /// Welcome email template
    /// </summary>
    public static class Welcome
    {
        public static string Subject => "Welcome to Restaurant System! 🍽️";

        public static string GetHtmlBody(string firstName, string lastName, string role)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Welcome</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #27ae60; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px 20px; background: #f9f9f9; }}
        .feature {{ background: white; padding: 15px; margin: 10px 0; border-radius: 5px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🍽️ Welcome to Restaurant System!</h1>
        </div>
        <div class='content'>
            <h2>Welcome aboard, {firstName}!</h2>
            <p>Congratulations! Your account has been successfully created with the role of <strong>{role}</strong>.</p>
            
            <div class='feature'>
                <h3>🔐 Your Account Security</h3>
                <p>Your account is protected with industry-standard security measures. Always keep your password safe and never share it with others.</p>
            </div>

            <div class='feature'>
                <h3>🚀 Getting Started</h3>
                <p>You can now log in to your account and start using all the features available to you based on your role.</p>
            </div>

            <div class='feature'>
                <h3>💡 Need Help?</h3>
                <p>If you have any questions or need assistance, our support team is here to help. Contact us anytime!</p>
            </div>

            <p>Thank you for joining Restaurant System. We're excited to have you on board!</p>
            <p>Best regards,<br>The Restaurant System Team</p>
        </div>
        <div class='footer'>
            <p>This is an automated message, please do not reply to this email.</p>
            <p>© 2024 Restaurant System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        public static string GetTextBody(string firstName, string lastName, string role)
        {
            return $@"Welcome to Restaurant System!

Welcome aboard, {firstName}!

Congratulations! Your account has been successfully created with the role of {role}.

Your Account Security:
Your account is protected with industry-standard security measures. Always keep your password safe and never share it with others.

Getting Started:
You can now log in to your account and start using all the features available to you based on your role.

Need Help?
If you have any questions or need assistance, our support team is here to help. Contact us anytime!

Thank you for joining Restaurant System. We're excited to have you on board!

Best regards,
The Restaurant System Team

This is an automated message, please do not reply to this email.
© 2024 Restaurant System. All rights reserved.";
        }
    }

    /// <summary>
    /// Password changed notification template
    /// </summary>
    public static class PasswordChanged
    {
        public static string Subject => "Password Changed - Restaurant System";

        public static string GetHtmlBody(string firstName, string lastName, DateTime changedAt)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Password Changed</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #e67e22; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px 20px; background: #f9f9f9; }}
        .alert {{ background: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Restaurant System</h1>
        </div>
        <div class='content'>
            <h2>Password Changed Successfully</h2>
            <p>Hello {firstName} {lastName},</p>
            <div class='alert'>
                <strong>✅ Your password has been successfully changed.</strong><br>
                Changed on: {changedAt:F}
            </div>
            <p>If you made this change, no further action is required.</p>
            <p><strong>If you didn't change your password:</strong></p>
            <ul>
                <li>Someone else may have access to your account</li>
                <li>Contact our support team immediately</li>
                <li>Consider changing your password again</li>
            </ul>
            <p>For your security, always use a strong, unique password and never share it with others.</p>
            <p>Best regards,<br>The Restaurant System Team</p>
        </div>
        <div class='footer'>
            <p>This is an automated message, please do not reply to this email.</p>
            <p>© 2024 Restaurant System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        public static string GetTextBody(string firstName, string lastName, DateTime changedAt)
        {
            return $@"Restaurant System - Password Changed

Hello {firstName} {lastName},

Your password has been successfully changed on {changedAt:F}.

If you made this change, no further action is required.

If you didn't change your password:
- Someone else may have access to your account
- Contact our support team immediately
- Consider changing your password again

For your security, always use a strong, unique password and never share it with others.

Best regards,
The Restaurant System Team

This is an automated message, please do not reply to this email.
© {DateTime.Now.Year} Restaurant System. All rights reserved.";
        }
    }

    /// <summary>
    /// Email verification template
    /// </summary>
    public static class EmailVerification
    {
        public static string Subject => "Verify Your Email - Restaurant System";

        public static string GetHtmlBody(string firstName, string lastName, string verificationUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Email Verification</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #9b59b6; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px 20px; background: #f9f9f9; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #9b59b6; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>📧 Restaurant System</h1>
        </div>
        <div class='content'>
            <h2>Verify Your Email Address</h2>
            <p>Hello {firstName} {lastName},</p>
            <p>Thank you for registering with Restaurant System! To complete your registration, please verify your email address.</p>
            <div style='text-align: center;'>
                <a href='{verificationUrl}' class='button'>Verify Email</a>
            </div>
            <p>Or copy and paste this link into your browser:</p>
            <p style='word-break: break-all;'>{verificationUrl}</p>
            <p>If you didn't create an account with us, please ignore this email.</p>
            <p>Best regards,<br>The Restaurant System Team</p>
        </div>
        <div class='footer'>
            <p>This is an automated message, please do not reply to this email.</p>
            <p>© 2024 Restaurant System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        public static string GetTextBody(string firstName, string lastName, string verificationUrl)
        {
            return $@"Restaurant System - Email Verification

Hello {firstName} {lastName},

Thank you for registering with Restaurant System! To complete your registration, please verify your email address by visiting the following link:

{verificationUrl}

If you didn't create an account with us, please ignore this email.

Best regards,
The Restaurant System Team

This is an automated message, please do not reply to this email.
© 2024 Restaurant System. All rights reserved.";
        }
    }
}