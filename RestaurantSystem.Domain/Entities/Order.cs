using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Domain.Common;
using RestaurantSystem.Domain.Common.Base;

namespace RestaurantSystem.Domain.Entities;
public class Order : SoftDeleteEntity
{
    public string OrderNumber { get; set; } = null!;
    public Guid? UserId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }

    // Order Type
    public OrderType Type { get; set; } // Dine-In, Takeaway, Delivery
    public int? TableNumber { get; set; } // For dine-in orders

    // Pricing
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Discount { get; set; }
    public decimal DiscountPercentage { get; set; } // Applied percentage
    public decimal Tip { get; set; }
    public decimal Total { get; set; }

    // Discount Details
    public string? PromoCode { get; set; }
    public bool HasUserLimitDiscount { get; set; }
    public decimal UserLimitAmount { get; set; } // Threshold for discount

    // Status
    public OrderStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }

    // Timestamps
    public DateTime OrderDate { get; set; }
    public DateTime? EstimatedDeliveryTime { get; set; }
    public DateTime? ActualDeliveryTime { get; set; }
    public DateTime? PaymentDate { get; set; }

    // Additional Info
    public string? Notes { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? CancellationReason { get; set; }

    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public virtual ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();

}
