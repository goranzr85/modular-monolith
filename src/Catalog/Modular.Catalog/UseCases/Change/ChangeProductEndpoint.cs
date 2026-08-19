using Carter;
using ErrorOr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modular.Authorization;
using Modular.Catalog.Authorization;
using Modular.Common;

namespace Modular.Catalog.UseCases.Change;
public sealed class ChangeProductEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/products", async (ChangeProductRequest request, ChangeProductCommandHandler handler, CancellationToken cancellationToken) =>
        {
            ChangeProductCommand command = new(request.Sku, request.Name, request.Description, request.Price);

            ErrorOr<Unit> response = await handler.Handle(command, cancellationToken);

            return response.ToResult((sku) => Results.Ok());
        })
       .WithName("ChangeProduct")
       .WithTags(Constants.EndpointTag)
       .Produces(StatusCodes.Status400BadRequest)
       .Produces(StatusCodes.Status500InternalServerError)
       .Produces(StatusCodes.Status201Created)
       .RequireAuthorization(policy => policy.RequirePermission(Permissions.CatalogUpdate));
    }
}
