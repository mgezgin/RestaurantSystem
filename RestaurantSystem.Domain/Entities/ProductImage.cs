﻿using RestaurantSystem.Domain.Common.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantSystem.Domain.Entities;
public class ProductImage : SoftDeleteEntity
{
    public string Url { get; set; } = null!;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; } = false;
    public int SortOrder { get; set; }

    // Foreign key
    public Guid ProductId { get; set; }

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
}
