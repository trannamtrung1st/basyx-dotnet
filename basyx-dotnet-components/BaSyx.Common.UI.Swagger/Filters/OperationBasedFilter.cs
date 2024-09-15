using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BaSyx.Common.UI.Swagger.Filters
{
    public abstract class OperationBasedFilter : IOperationFilter
    {
        public abstract string OperationId { get; }
        public abstract string RequestExampleJson { get; }
        public abstract Dictionary<string, string> RequestExampleJsons { get; }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.OperationId == OperationId)
            {
                foreach (var content in operation.RequestBody.Content)
                {
                    if (!content.Key.Contains("json", System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (RequestExampleJson != null)
                        content.Value.Example = OpenApiAnyFactory.CreateFromJson(RequestExampleJson);
                    else if (RequestExampleJsons?.Count > 0)
                    {
                        content.Value.Examples = RequestExampleJsons.ToDictionary(
                            kvp => kvp.Key,
                            kvp => new OpenApiExample
                            {
                                Value = OpenApiAnyFactory.CreateFromJson(kvp.Value)
                            }
                        );
                    }
                }
            }
        }
    }
}