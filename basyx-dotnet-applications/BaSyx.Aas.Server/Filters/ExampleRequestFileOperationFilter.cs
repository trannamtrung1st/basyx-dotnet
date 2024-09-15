using System.Collections.Generic;
using System.IO;
using BaSyx.Aas.Server.Services;
using BaSyx.Common.UI.Swagger.Filters;

namespace BaSyx.Aas.Server.Filters;

public abstract class ExampleRequestFileOperationFilter : OperationBasedFilter
{
    protected string FilePrefix => $"./Samples/Requests/{OperationId}";
    protected string JsonFile => $"{FilePrefix}.json";
    protected string ExamplesDirectory => FilePrefix;
    public override string RequestExampleJson => File.Exists(JsonFile)
        ? CachedFileService.Global?.GetFileContentsAsync(JsonFile).Result
        : null;

    public override Dictionary<string, string> RequestExampleJsons
    {
        get
        {
            if (Directory.Exists(ExamplesDirectory))
            {
                var examples = new Dictionary<string, string>();
                var files = Directory.GetFiles(ExamplesDirectory);
                foreach (var file in files)
                    examples[Path.GetFileNameWithoutExtension(file)] = CachedFileService.Global.GetFileContentsAsync(file).Result;
                return examples;
            }
            return null;
        }
    }
}
