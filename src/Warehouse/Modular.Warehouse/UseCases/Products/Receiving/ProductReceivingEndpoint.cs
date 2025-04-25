using Carter;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modular.Common;

namespace Modular.Warehouse.UseCases.Products.Receiving;
public sealed class ProductReceivingEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/warehouse/received/{sku}", async (string sku, ProductReceivingRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            ProductReceivingCommand command = new(sku, request.Quantity);

            ErrorOr<Unit> response = await sender.Send(command, cancellationToken);

            return response.ToResult(_ => Results.NoContent());
        })
        .WithName("ReceivedProduct")
        .WithTags("Warehouse")
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError)
        .Produces(StatusCodes.Status200OK);
    }
}
