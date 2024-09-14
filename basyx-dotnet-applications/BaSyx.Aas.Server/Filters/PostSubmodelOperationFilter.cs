using BaSyx.API.Http.Controllers;

namespace BaSyx.Aas.Server.Filters;

public class PostSubmodelOperationFilter : ExampleRequestFileOperationFilter
{
    public override string OperationId => nameof(SubmodelRepositoryController.PostSubmodel);
}