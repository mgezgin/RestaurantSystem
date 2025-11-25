namespace RestaurantSystem.Api.Common.Templates;

public static partial class EmailTemplates
{
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
