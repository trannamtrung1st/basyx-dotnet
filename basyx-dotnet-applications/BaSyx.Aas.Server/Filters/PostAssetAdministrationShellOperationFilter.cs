using BaSyx.API.Http.Controllers;

namespace BaSyx.Aas.Server.Filters;

public class PostAssetAdministrationShellOperationFilter : ExampleRequestFileOperationFilter
{
    public override string OperationId => nameof(AssetAdministrationShellRepositoryController.PostAssetAdministrationShell);
}