using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BaSyx.Common.UI.Swagger.Filters
{
    public abstract class OperationBasedFilter : IOperationFilter
    {
        public abstract string OperationId { get; }
        public abstract string RequestExampleJson { get; }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.OperationId == OperationId)
            {
                foreach (var content in operation.RequestBody.Content)
                {
                    if (!content.Key.Contains("json", System.StringComparison.OrdinalIgnoreCase))
                        continue;
                    content.Value.Example = OpenApiAnyFactory.CreateFromJson(RequestExampleJson);
                }
            }
        }
    }
}