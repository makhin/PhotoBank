using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PhotoBank.DependencyInjection.Swagger
{
    public sealed class StringEnumSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            var type = context.Type;
            if (!type.IsEnum) return;

            schema.Type = "string";
            schema.Format = null;

            var values = Enum.GetValues(type).Cast<Enum>()
                .Select(v =>
                {
                    var name = v.ToString();
                    var mem = type.GetMember(name).FirstOrDefault();
                    var enumMember = mem?.GetCustomAttribute<EnumMemberAttribute>();
                    var wireName = enumMember?.Value ?? name;
                    return (IOpenApiAny)new OpenApiString(wireName);
                })
                .ToList();

            schema.Enum = values;
        }
    }
}
