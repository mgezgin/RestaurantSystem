namespace RestaurantSystem.Api.Common.Templates;

public static partial class EmailTemplates
{
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
}
