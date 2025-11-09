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

    /// <summary>
    /// Reservation confirmation email template (sent to customer and admin)
    /// </summary>
    public static class ReservationConfirmation
    {
        public static string Subject => "Reservation Confirmation - Rumi Restaurant";

        public static string GetHtmlBody(string customerName, string tableNumber, DateTime reservationDate,
            TimeSpan startTime, TimeSpan endTime, int numberOfGuests, string? specialRequests = null)
        {
            var requestsSection = string.IsNullOrEmpty(specialRequests)
                ? ""
                : $@"<div class='info-box'>
                        <strong>Special Requests:</strong><br>
                        {specialRequests}
                    </div>";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reservation Confirmation</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #d4af37; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px 20px; background: #f9f9f9; }}
        .info-box {{ background: white; padding: 15px; margin: 15px 0; border-radius: 5px; border-left: 4px solid #d4af37; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
        .pending {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🍽️ Rumi Restaurant</h1>
        </div>
        <div class='content'>
            <h2>Reservation Received</h2>
            <p>Dear {customerName},</p>
            <p>Thank you for your reservation request at Rumi Restaurant. We have received your booking details:</p>

            <div class='info-box'>
                <strong>📅 Date:</strong> {reservationDate:dddd, MMMM dd, yyyy}<br>
                <strong>🕐 Time:</strong> {startTime:hh':'mm} - {endTime:hh':'mm}<br>
                <strong>👥 Guests:</strong> {numberOfGuests}<br>
                <strong>🪑 Table:</strong> {tableNumber}
            </div>

            {requestsSection}

            <div class='pending'>
                <strong>⏳ Pending Confirmation</strong><br>
                Your reservation is currently pending. Our team will review your request and send you a confirmation email shortly.
            </div>

            <p>If you need to make any changes or have questions, please contact us at rumigeneve@gmail.com</p>
            <p>We look forward to serving you!</p>
            <p>Best regards,<br>Rumi Restaurant Team</p>
        </div>
        <div class='footer'>
            <p>Rumi Restaurant | Geneva | rumigeneve@gmail.com</p>
            <p>© 2024 Rumi Restaurant. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        public static string GetTextBody(string customerName, string tableNumber, DateTime reservationDate,
            TimeSpan startTime, TimeSpan endTime, int numberOfGuests, string? specialRequests = null)
        {
            var requestsSection = string.IsNullOrEmpty(specialRequests)
                ? ""
                : $@"

Special Requests:
{specialRequests}";

            return $@"Rumi Restaurant - Reservation Received

Dear {customerName},

Thank you for your reservation request at Rumi Restaurant. We have received your booking details:

Date: {reservationDate:dddd, MMMM dd, yyyy}
Time: {startTime:hh':'mm} - {endTime:hh':'mm}
Guests: {numberOfGuests}
Table: {tableNumber}{requestsSection}

PENDING CONFIRMATION
Your reservation is currently pending. Our team will review your request and send you a confirmation email shortly.

If you need to make any changes or have questions, please contact us at rumigeneve@gmail.com

We look forward to serving you!

Best regards,
Rumi Restaurant Team

Rumi Restaurant | Geneva | rumigeneve@gmail.com
© 2024 Rumi Restaurant. All rights reserved.";
        }
    }

    /// <summary>
    /// Reservation approved email template (sent to customer)
    /// </summary>
    public static class ReservationApproved
    {
        public static string Subject => "Reservation Confirmed - Rumi Restaurant";

        public static string GetHtmlBody(string customerName, string tableNumber, DateTime reservationDate,
            TimeSpan startTime, TimeSpan endTime, int numberOfGuests, string? specialRequests = null, string? notes = null)
        {
            var requestsSection = string.IsNullOrEmpty(specialRequests)
                ? ""
                : $@"<div class='info-box'>
                        <strong>Special Requests:</strong><br>
                        {specialRequests}
                    </div>";

            var notesSection = string.IsNullOrEmpty(notes)
                ? ""
                : $@"<div class='info-box' style='border-left-color: #27ae60;'>
                        <strong>Note from Restaurant:</strong><br>
                        {notes}
                    </div>";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reservation Confirmed</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #27ae60; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px 20px; background: #f9f9f9; }}
        .info-box {{ background: white; padding: 15px; margin: 15px 0; border-radius: 5px; border-left: 4px solid #27ae60; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
        .confirmed {{ background: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 5px; margin: 20px 0; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🍽️ Rumi Restaurant</h1>
        </div>
        <div class='content'>
            <div class='confirmed'>
                <h2 style='margin: 0; color: #27ae60;'>✅ Reservation Confirmed!</h2>
            </div>

            <p>Dear {customerName},</p>
            <p>Great news! Your reservation at Rumi Restaurant has been confirmed.</p>

            <div class='info-box'>
                <strong>📅 Date:</strong> {reservationDate:dddd, MMMM dd, yyyy}<br>
                <strong>🕐 Time:</strong> {startTime:hh':'mm} - {endTime:hh':'mm}<br>
                <strong>👥 Guests:</strong> {numberOfGuests}<br>
                <strong>🪑 Table:</strong> {tableNumber}
            </div>

            {requestsSection}
            {notesSection}

            <p><strong>Important Information:</strong></p>
            <ul>
                <li>Please arrive on time. Tables are held for 15 minutes past reservation time.</li>
                <li>If you need to cancel or modify your reservation, please contact us at least 24 hours in advance.</li>
                <li>Contact us at: rumigeneve@gmail.com</li>
            </ul>

            <p>We look forward to welcoming you!</p>
            <p>Best regards,<br>Rumi Restaurant Team</p>
        </div>
        <div class='footer'>
            <p>Rumi Restaurant | Geneva | rumigeneve@gmail.com</p>
            <p>© 2024 Rumi Restaurant. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        public static string GetTextBody(string customerName, string tableNumber, DateTime reservationDate,
            TimeSpan startTime, TimeSpan endTime, int numberOfGuests, string? specialRequests = null, string? notes = null)
        {
            var requestsSection = string.IsNullOrEmpty(specialRequests)
                ? ""
                : $@"

Special Requests:
{specialRequests}";

            var notesSection = string.IsNullOrEmpty(notes)
                ? ""
                : $@"

Note from Restaurant:
{notes}";

            return $@"Rumi Restaurant - Reservation Confirmed

✅ RESERVATION CONFIRMED!

Dear {customerName},

Great news! Your reservation at Rumi Restaurant has been confirmed.

Date: {reservationDate:dddd, MMMM dd, yyyy}
Time: {startTime:hh':'mm} - {endTime:hh':'mm}
Guests: {numberOfGuests}
Table: {tableNumber}{requestsSection}{notesSection}

Important Information:
- Please arrive on time. Tables are held for 15 minutes past reservation time.
- If you need to cancel or modify your reservation, please contact us at least 24 hours in advance.
- Contact us at: rumigeneve@gmail.com

We look forward to welcoming you!

Best regards,
Rumi Restaurant Team

Rumi Restaurant | Geneva | rumigeneve@gmail.com
© 2024 Rumi Restaurant. All rights reserved.";
        }
    }

    /// <summary>
    /// Order confirmation email template (sent to customer)
    /// </summary>
    public static class OrderConfirmation
    {
        public static string Subject => "Order Confirmation - Rumi Restaurant";

        public static string GetHtmlBody(string customerName, string orderNumber, string orderType, decimal total,
            IEnumerable<(string name, int quantity, decimal price)> items, string? specialInstructions = null,
            string? deliveryAddress = null)
        {
            var itemsSection = string.Join("", items.Select(item =>
                $@"<tr>
                    <td style='padding: 10px; border-bottom: 1px solid #eee;'>{item.name}</td>
                    <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: center;'>x{item.quantity}</td>
                    <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: right;'>CHF {item.price:F2}</td>
                </tr>"));

            var instructionsSection = string.IsNullOrEmpty(specialInstructions)
                ? ""
                : $@"<div class='info-box'>
                        <strong>Special Instructions:</strong><br>
                        {specialInstructions}
                    </div>";

            var deliverySection = string.IsNullOrEmpty(deliveryAddress)
                ? ""
                : $@"<div class='info-box'>
                        <strong>📍 Delivery Address:</strong><br>
                        {deliveryAddress}
                    </div>";

            var orderTypeEmoji = orderType switch
            {
                "DineIn" => "🍽️ Dine In",
                "Takeaway" => "🛍️ Takeaway",
                "Delivery" => "🚚 Delivery",
                _ => orderType
            };

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Order Confirmation</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #d4af37; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px 20px; background: #f9f9f9; }}
        .info-box {{ background: white; padding: 15px; margin: 15px 0; border-radius: 5px; border-left: 4px solid #d4af37; }}
        .order-number {{ background: linear-gradient(135deg, #d4af37 0%, #f4c430 100%); color: white; padding: 20px; border-radius: 5px; text-align: center; margin: 20px 0; }}
        .order-number-value {{ font-size: 28px; font-weight: bold; letter-spacing: 2px; }}
        .order-number-label {{ font-size: 14px; opacity: 0.9; margin-top: 5px; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; background: white; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
        .success {{ background: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 5px; margin: 20px 0; color: #155724; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🍽️ Rumi Restaurant</h1>
        </div>
        <div class='content'>
            <h2>✅ Order Confirmed!</h2>
            <p>Dear {customerName},</p>
            <p>Thank you for your order at Rumi Restaurant! We have received your order and will start preparing it shortly.</p>

            <div class='order-number'>
                <div class='order-number-label'>ORDER NUMBER</div>
                <div class='order-number-value'>{orderNumber}</div>
            </div>

            <div class='info-box'>
                <strong>📦 Order Type:</strong> {orderTypeEmoji}<br>
                <strong>💰 Total Amount:</strong> CHF {total:F2}
            </div>

            <h3>Order Items:</h3>
            <table>
                <thead>
                    <tr style='background: #f5f5f5;'>
                        <th style='padding: 10px; text-align: left;'>Item</th>
                        <th style='padding: 10px; text-align: center;'>Qty</th>
                        <th style='padding: 10px; text-align: right;'>Price</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsSection}
                </tbody>
            </table>

            {deliverySection}
            {instructionsSection}

            <div class='success'>
                <strong>⏱️ Expected Preparation Time:</strong><br>
                We estimate your order will be ready in {GetEstimatedTime(orderType)}.
            </div>

            <p>You can track your order status in your account. If you have any questions, please contact us at rumigeneve@gmail.com</p>
            <p>We look forward to serving you!</p>
            <p>Best regards,<br>Rumi Restaurant Team</p>
        </div>
        <div class='footer'>
            <p>Rumi Restaurant | Geneva | rumigeneve@gmail.com</p>
            <p>© 2024 Rumi Restaurant. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        public static string GetTextBody(string customerName, string orderNumber, string orderType, decimal total,
            IEnumerable<(string name, int quantity, decimal price)> items, string? specialInstructions = null,
            string? deliveryAddress = null)
        {
            var itemsSection = string.Join("\n", items.Select(item =>
                $"{item.name} x{item.quantity} = CHF {item.price:F2}"));

            var instructionsSection = string.IsNullOrEmpty(specialInstructions)
                ? ""
                : $@"

Special Instructions:
{specialInstructions}";

            var deliverySection = string.IsNullOrEmpty(deliveryAddress)
                ? ""
                : $@"

Delivery Address:
{deliveryAddress}";

            var orderTypeText = orderType switch
            {
                "DineIn" => "Dine In",
                "Takeaway" => "Takeaway",
                "Delivery" => "Delivery",
                _ => orderType
            };

            return $@"Rumi Restaurant - Order Confirmation

✅ ORDER CONFIRMED!

Dear {customerName},

Thank you for your order at Rumi Restaurant! We have received your order and will start preparing it shortly.

ORDER NUMBER: {orderNumber}

Order Type: {orderTypeText}
Total Amount: CHF {total:F2}

Order Items:
{itemsSection}{deliverySection}{instructionsSection}

ESTIMATED PREPARATION TIME: {GetEstimatedTime(orderType)}

You can track your order status in your account. If you have any questions, please contact us at rumigeneve@gmail.com

We look forward to serving you!

Best regards,
Rumi Restaurant Team

Rumi Restaurant | Geneva | rumigeneve@gmail.com
© 2024 Rumi Restaurant. All rights reserved.";
        }

        private static string GetEstimatedTime(string orderType)
        {
            return orderType switch
            {
                "DineIn" => "15-20 minutes",
                "Takeaway" => "20-25 minutes",
                "Delivery" => "30-45 minutes",
                _ => "20-30 minutes"
            };
        }
    }

    /// <summary>
    /// Order confirmation email template (sent to admin/restaurant)
    /// </summary>
    public static class OrderConfirmationAdmin
    {
        public static string Subject => "New Order - Rumi Restaurant";

        public static string GetHtmlBody(string orderNumber, string customerName, string customerEmail, string customerPhone,
            string orderType, decimal total, IEnumerable<(string name, int quantity, decimal price)> items,
            string? specialInstructions = null, string? deliveryAddress = null)
        {
            var itemsSection = string.Join("", items.Select(item =>
                $@"<tr>
                    <td style='padding: 10px; border-bottom: 1px solid #eee;'>{item.name}</td>
                    <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: center;'>x{item.quantity}</td>
                    <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: right;'>CHF {item.price:F2}</td>
                </tr>"));

            var instructionsSection = string.IsNullOrEmpty(specialInstructions)
                ? ""
                : $@"<div class='info-box'>
                        <strong>Special Instructions:</strong><br>
                        {specialInstructions}
                    </div>";

            var deliverySection = string.IsNullOrEmpty(deliveryAddress)
                ? ""
                : $@"<div class='info-box'>
                        <strong>📍 Delivery Address:</strong><br>
                        {deliveryAddress}
                    </div>";

            var orderTypeEmoji = orderType switch
            {
                "DineIn" => "🍽️ Dine In",
                "Takeaway" => "🛍️ Takeaway",
                "Delivery" => "🚚 Delivery",
                _ => orderType
            };

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>New Order</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #d4af37; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px 20px; background: #f9f9f9; }}
        .info-box {{ background: white; padding: 15px; margin: 15px 0; border-radius: 5px; border-left: 4px solid #d4af37; }}
        .order-number {{ background: linear-gradient(135deg, #d4af37 0%, #f4c430 100%); color: white; padding: 20px; border-radius: 5px; text-align: center; margin: 20px 0; }}
        .order-number-value {{ font-size: 28px; font-weight: bold; letter-spacing: 2px; }}
        .order-number-label {{ font-size: 14px; opacity: 0.9; margin-top: 5px; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; background: white; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
        .alert {{ background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🍽️ Rumi Restaurant</h1>
        </div>
        <div class='content'>
            <h2>📦 New Order Received</h2>
            <p>A new order has been placed in the system.</p>

            <div class='order-number'>
                <div class='order-number-label'>ORDER NUMBER</div>
                <div class='order-number-value'>{orderNumber}</div>
            </div>

            <div class='info-box'>
                <strong>👤 Customer:</strong> {customerName}<br>
                <strong>📧 Email:</strong> {customerEmail}<br>
                <strong>📱 Phone:</strong> {customerPhone}
            </div>

            <div class='info-box'>
                <strong>📦 Order Type:</strong> {orderTypeEmoji}<br>
                <strong>💰 Total Amount:</strong> CHF {total:F2}
            </div>

            <h3>Order Items:</h3>
            <table>
                <thead>
                    <tr style='background: #f5f5f5;'>
                        <th style='padding: 10px; text-align: left;'>Item</th>
                        <th style='padding: 10px; text-align: center;'>Qty</th>
                        <th style='padding: 10px; text-align: right;'>Price</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsSection}
                </tbody>
            </table>

            {deliverySection}
            {instructionsSection}

            <div class='alert'>
                <strong>⚠️ Action Required:</strong><br>
                Please prepare this order for {(orderType == "Delivery" ? "delivery" : orderType == "Takeaway" ? "takeaway" : "serving")}.
            </div>

            <p>Log in to your admin dashboard to manage this order.</p>
            <p>Best regards,<br>Restaurant System</p>
        </div>
        <div class='footer'>
            <p>Rumi Restaurant | Geneva | rumigeneve@gmail.com</p>
            <p>© 2024 Rumi Restaurant. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        public static string GetTextBody(string orderNumber, string customerName, string customerEmail, string customerPhone,
            string orderType, decimal total, IEnumerable<(string name, int quantity, decimal price)> items,
            string? specialInstructions = null, string? deliveryAddress = null)
        {
            var itemsSection = string.Join("\n", items.Select(item =>
                $"{item.name} x{item.quantity} = CHF {item.price:F2}"));

            var instructionsSection = string.IsNullOrEmpty(specialInstructions)
                ? ""
                : $@"

Special Instructions:
{specialInstructions}";

            var deliverySection = string.IsNullOrEmpty(deliveryAddress)
                ? ""
                : $@"

Delivery Address:
{deliveryAddress}";

            var orderTypeText = orderType switch
            {
                "DineIn" => "Dine In",
                "Takeaway" => "Takeaway",
                "Delivery" => "Delivery",
                _ => orderType
            };

            return $@"Rumi Restaurant - New Order

📦 NEW ORDER RECEIVED

Order Number: {orderNumber}

Customer: {customerName}
Email: {customerEmail}
Phone: {customerPhone}

Order Type: {orderTypeText}
Total Amount: CHF {total:F2}

Order Items:
{itemsSection}{deliverySection}{instructionsSection}

ACTION REQUIRED:
Please prepare this order for {(orderType == "Delivery" ? "delivery" : orderType == "Takeaway" ? "takeaway" : "serving")}.

Log in to your admin dashboard to manage this order.

Best regards,
Restaurant System

Rumi Restaurant | Geneva | rumigeneve@gmail.com
© 2024 Rumi Restaurant. All rights reserved.";
        }
    }
}