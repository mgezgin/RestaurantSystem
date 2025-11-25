namespace RestaurantSystem.Api.Common.Templates;

public static partial class EmailTemplates
{
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
        .action-buttons {{ text-align: center; margin: 30px 0; }}
        .button {{ display: inline-block; padding: 12px 24px; margin: 0 10px; text-decoration: none; border-radius: 5px; font-weight: bold; }}
        .button-primary {{ background: #7fa89bff; color: white; }}
        .button-secondary {{ background: #2563eb; color: white; }}
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
                Please confirm or cancel this order.
            </div>

            <div class='action-buttons'>
                <a href='http://localhost:5221/api/Orders/{orderNumber}/quick-confirm?minutes=0' class='button button-primary' style='background: #047857;'>✓ Confirm Now</a>
            </div>

            <div class='action-buttons' style='margin-top: 15px;'>
                <p style='margin-bottom: 10px; font-size: 14px; color: #666;'>Or confirm with preparation time:</p>
                <a href='http://localhost:5221/api/Orders/{orderNumber}/quick-confirm?minutes=15' class='button button-primary'>15 min</a>
                <a href='http://localhost:5221/api/Orders/{orderNumber}/quick-confirm?minutes=30' class='button button-secondary'>30 min</a>
                <a href='http://localhost:5221/api/Orders/{orderNumber}/quick-confirm?minutes=45' class='button button-secondary'>45 min</a>
            </div>

            <div class='action-buttons' style='margin-top: 20px;'>
                <a href='http://localhost:5221/api/Orders/{orderNumber}/quick-cancel' class='button' style='background: #dc2626; color: white;'>✕ Cancel Order</a>
            </div>

            <p style='text-align: center; margin-top: 20px; font-size: 12px; color: #666;'>
                Need a different time? <a href='http://localhost:3000/admin/orders-management' style='color: #2563eb;'>Open dashboard</a> for custom preparation time
            </p>

            <p>The customer will be notified automatically after you take action.</p>
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
