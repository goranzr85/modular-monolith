using Carter;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modular.Common;

namespace Modular.Catalog.Change;
public sealed class ChangeProductEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/products", async (ChangeProductRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            ChangeProductCommand command = new(request.Sku, request.Name, request.Description, request.Price);

            ErrorOr<Unit> response = await sender.Send(command, cancellationToken);

            return response.ToResult((sku) => Results.Ok());
        })
       .WithName("ChangeProduct")
       .WithTags("Catalogs")
       .Produces(StatusCodes.Status400BadRequest)
       .Produces(StatusCodes.Status500InternalServerError)
       .Produces(StatusCodes.Status201Created);
    }
}
