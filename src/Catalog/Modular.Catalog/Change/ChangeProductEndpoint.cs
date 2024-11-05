using Carter;
using ErrorOr;
using MediatR;
using Modular.Common;

namespace Modular.Catalog.Change;
public sealed class ChangeProductEndpoint : ICarterModule
{

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/products", async (ChangeProductRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            ChangeProductCommand command = new(request.Id, request.Sku, request.Name, request.Description, request.Price);

            ErrorOr<Unit> response = await sender.Send(command, cancellationToken);

            return response.ToResult((productId) => Results.Created($"/api/products/{productId}", productId));
        })
        .WithName("CreateProduct")
        .WithTags("Catalogs")
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError)
        .Produces(StatusCodes.Status201Created);
    }

}
