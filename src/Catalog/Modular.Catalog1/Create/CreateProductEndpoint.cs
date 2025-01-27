using Carter;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modular.Common;

namespace Modular.Catalog.Create;
public sealed class CreateProductEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/products", async (CreateProductRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            CreateProductCommand command = new(request.Sku, request.Name, request.Description, request.Price);

            ErrorOr<Unit> response = await sender.Send(command, cancellationToken);

            return response.ToResult((sku) => Results.Created($"/api/products/{sku}", sku));
        })
        .WithName("CreateProduct")
        .WithTags("Catalogs")
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError)
        .Produces(StatusCodes.Status201Created);
    }
}
