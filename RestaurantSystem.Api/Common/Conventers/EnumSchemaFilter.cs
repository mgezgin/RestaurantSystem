using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Runtime.Serialization;

namespace RestaurantSystem.Api.Common.Conventers;

/// <summary>
/// Swagger schema filter to show enums as strings with EnumMember values in documentation
/// </summary>
public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            schema.Enum.Clear();
            schema.Type = "string";
            schema.Format = null;

            var enumValues = new List<IOpenApiAny>();

            foreach (var enumValue in Enum.GetValues(context.Type))
            {
                var enumMember = context.Type.GetMember(enumValue.ToString()!).FirstOrDefault();
                var enumMemberAttribute = enumMember?.GetCustomAttributes(typeof(EnumMemberAttribute), false)
                    .Cast<EnumMemberAttribute>().FirstOrDefault();

                var value = enumMemberAttribute?.Value ?? enumValue.ToString()!;
                enumValues.Add(new OpenApiString(value));
            }

            schema.Enum = enumValues;
        }
    }
}
