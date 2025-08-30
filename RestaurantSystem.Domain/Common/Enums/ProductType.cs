using System.Runtime.Serialization;
namespace RestaurantSystem.Domain.Common.Enums;
public enum ProductType
{
    [EnumMember(Value = "mainItem")]
    MainItem = 0,

    [EnumMember(Value = "sideItem")]
    SideItem = 1,

    [EnumMember(Value = "beverage")]
    Beverage = 2,

    [EnumMember(Value = "dessert")]
    Dessert = 3,

    [EnumMember(Value = "sauce")]
    Sauce = 4,

    [EnumMember(Value = "addOn")]
    AddOn = 5
}
