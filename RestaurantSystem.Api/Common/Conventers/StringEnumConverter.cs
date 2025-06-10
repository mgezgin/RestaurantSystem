using System.Text.Json.Serialization;
using System.Text.Json;

namespace RestaurantSystem.Api.Common.Conventers;

/// <summary>
/// Custom JSON converter that serializes enums as strings for API responses
/// but accepts both strings and integers for deserialization from API requests
/// </summary>
public class StringEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                {
                    var stringValue = reader.GetString();
                    if (Enum.TryParse<T>(stringValue, true, out var enumValue))
                    {
                        return enumValue;
                    }
                    throw new JsonException($"Unable to convert \"{stringValue}\" to enum {typeof(T).Name}");
                }
            case JsonTokenType.Number:
                {
                    var intValue = reader.GetInt32();
                    if (Enum.IsDefined(typeof(T), intValue))
                    {
                        return (T)Enum.ToObject(typeof(T), intValue);
                    }
                    throw new JsonException($"Unable to convert {intValue} to enum {typeof(T).Name}");
                }
            default:
                throw new JsonException($"Unexpected token type {reader.TokenType} when reading enum {typeof(T).Name}");
        }
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        // Always write enums as strings to the frontend
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// Generic factory for creating string enum converters
/// </summary>
public class StringEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum ||
               (typeToConvert.IsGenericType &&
                typeToConvert.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                Nullable.GetUnderlyingType(typeToConvert)?.IsEnum == true);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type enumType = typeToConvert;
        bool isNullable = false;

        if (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            enumType = Nullable.GetUnderlyingType(typeToConvert)!;
            isNullable = true;
        }

        var converterType = isNullable
            ? typeof(NullableStringEnumConverter<>).MakeGenericType(enumType)
            : typeof(StringEnumConverter<>).MakeGenericType(enumType);

        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
