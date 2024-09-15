using BaSyx.API.Http.Controllers;

namespace BaSyx.Aas.Server.Filters;

public class SubmodelRepo_PostSubmodelElement : ExampleRequestFileOperationFilter
{
    public override string OperationId => nameof(SubmodelRepositoryController.SubmodelRepo_PostSubmodelElement);
}