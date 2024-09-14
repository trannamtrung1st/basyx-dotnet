using BaSyx.API.Http.Controllers;

namespace BaSyx.Aas.Server.Filters;

public class ShellRepo_PostSubmodelReferenceOperationFilter : ExampleRequestFileOperationFilter
{
    public override string OperationId => nameof(AssetAdministrationShellRepositoryController.ShellRepo_PostSubmodelReference);
}