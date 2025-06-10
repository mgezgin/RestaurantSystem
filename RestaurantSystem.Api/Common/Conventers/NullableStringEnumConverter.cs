using System.Text.Json.Serialization;
using System.Text.Json;

namespace RestaurantSystem.Api.Common.Conventers;

/// <summary>
/// String enum converter for nullable enums
/// </summary>
public class NullableStringEnumConverter<T> : JsonConverter<T?> where T : struct, Enum
{
    private readonly StringEnumConverter<T> _baseConverter = new();

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return _baseConverter.Read(ref reader, typeof(T), options);
    }

    public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            _baseConverter.Write(writer, value.Value, options);
        }
    }
}
