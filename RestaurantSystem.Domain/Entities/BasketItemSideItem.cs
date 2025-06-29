using RestaurantSystem.Domain.Common.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantSystem.Domain.Entities;
public class BasketItemSideItem : Entity
{
    public Guid BasketItemId { get; set; }
    public Guid SideItemProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // Navigation properties
    public virtual BasketItem BasketItem { get; set; } = null!;
    public virtual Product SideItemProduct { get; set; } = null!;
}
