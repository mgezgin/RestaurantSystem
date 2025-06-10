using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantSystem.Domain.Common.Enums;
public enum ProductType
{
    [EnumMember(Value = "main_item")]
    MainItem = 0,

    [EnumMember(Value = "side_item")]
    SideItem = 1,

    [EnumMember(Value = "beverage")]
    Beverage = 2,

    [EnumMember(Value = "dessert")]
    Dessert = 3,

    [EnumMember(Value = "sauce")]
    Sauce = 4,

    [EnumMember(Value = "add_on")]
    AddOn = 5
}
