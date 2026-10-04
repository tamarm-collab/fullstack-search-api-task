using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Requests.Api.Swagger;

/// <summary>
/// מוסיף פרמטרי Header לכל הקריאות ב-Swagger UI
/// </summary>
public class AddHeaderParameters : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        // X-User-Id - מזהה המשתמש
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-User-Id",
            In = ParameterLocation.Header,
            Description = "מזהה המשתמש (מספר, למשל: 1)",
            Required = true,
            Schema = new OpenApiSchema { Type = "integer", Default = new Microsoft.OpenApi.Any.OpenApiInteger(1) }
        });

        // X-Is-Admin - האם מנהל
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Is-Admin",
            In = ParameterLocation.Header,
            Description = "האם המשתמש מנהל (true/false)",
            Required = false,
            Schema = new OpenApiSchema { Type = "boolean", Default = new Microsoft.OpenApi.Any.OpenApiBoolean(false) }
        });
    }
}
