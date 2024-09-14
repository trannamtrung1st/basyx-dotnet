using BaSyx.Aas.Server.Services;
using BaSyx.Common.UI.Swagger.Filters;

namespace BaSyx.Aas.Server.Filters;

public abstract class ExampleRequestFileOperationFilter : OperationBasedFilter
{
    public override string RequestExampleJson => CachedFileService.Global?.GetFileContentsAsync($"./Samples/Requests/{OperationId}.json").Result;
}
