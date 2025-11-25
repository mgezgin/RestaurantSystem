namespace RestaurantSystem.Api.Common.Templates;

public static partial class EmailTemplates
{
    /// <summary>
    /// Order delayed email template (sent to customer)
    /// </summary>
    public static class OrderDelayed
    {
        public static string Subject => "Action Required: Order Delay - Rumi Restaurant";

        public static string GetHtmlBody(string customerName, string orderNumber, int delayMinutes, string approveUrl, string rejectUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Order Delayed</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #f59e0b; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px 20px; background: #f9f9f9; }}
        .info-box {{ background: white; padding: 15px; margin: 15px 0; border-radius: 5px; border-left: 4px solid #f59e0b; }}
        .order-number {{ background: #fef3c7; color: #92400e; padding: 20px; border-radius: 5px; text-align: center; margin: 20px 0; }}
        .order-number-value {{ font-size: 28px; font-weight: bold; letter-spacing: 2px; }}
        .order-number-label {{ font-size: 14px; margin-top: 5px; }}
        .actions {{ text-align: center; margin: 30px 0; }}
        .btn {{ display: inline-block; padding: 12px 24px; margin: 0 10px; text-decoration: none; border-radius: 5px; font-weight: bold; }}
        .btn-accept {{ background: #10b981; color: white; }}
        .btn-reject {{ background: #ef4444; color: white; }}
        .footer {{ padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🍽️ Rumi Restaurant</h1>
        </div>
        <div class='content'>
            <h2>Action Required: Order Delay</h2>
            <p>Dear {customerName},</p>
            <p>Thank you for your order. Due to high demand, we need a bit more time to prepare your delicious meal.</p>

            <div class='order-number'>
                <div class='order-number-label'>ORDER NUMBER</div>
                <div class='order-number-value'>{orderNumber}</div>
            </div>

            <div class='info-box'>
                <strong>Proposed Preparation Time:</strong><br>
                We estimate your order will be ready in approximately <strong>{delayMinutes} minutes</strong>.
            </div>

            <p>Please let us know if this works for you by clicking one of the buttons below:</p>

            <div class='actions'>
                <a href='{approveUrl}' class='btn btn-accept'>Accept Delay</a>
                <a href='{rejectUrl}' class='btn btn-reject'>Cancel Order</a>
            </div>

            <p>If you choose to cancel, you will not be charged.</p>
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

        public static string GetTextBody(string customerName, string orderNumber, int delayMinutes, string approveUrl, string rejectUrl)
        {
            return $@"Rumi Restaurant - Action Required: Order Delay

Dear {customerName},

Thank you for your order. Due to high demand, we need a bit more time to prepare your delicious meal.

Order Number: {orderNumber}

Proposed Preparation Time:
We estimate your order will be ready in approximately {delayMinutes} minutes.

Please let us know if this works for you:

Accept Delay: {approveUrl}

Cancel Order: {rejectUrl}

If you choose to cancel, you will not be charged.

Best regards,
Rumi Restaurant Team

Rumi Restaurant | Geneva | rumigeneve@gmail.com
© 2024 Rumi Restaurant. All rights reserved.";
        }
    }
}
