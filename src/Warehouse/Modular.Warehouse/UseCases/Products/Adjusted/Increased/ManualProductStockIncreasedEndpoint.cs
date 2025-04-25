using Carter;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modular.Common;
using Modular.Warehouse.UseCases.Products.Adjusted;

namespace Modular.Warehouse.UseCases.Products.Adjusted.Increased;
public sealed class ManualProductStockIncreasedEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/warehouse/increased/{sku}", async (string sku, ProductAdjustedRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            ManualProductStockIncreaseCommand command = new(sku, request.Quantity, request.Reason);

            ErrorOr<Unit> response = await sender.Send(command, cancellationToken);

            return response.ToResult(_ => Results.NoContent());
        })
        .WithName("ManuallyIncreaseProduct")
        .WithTags("Warehouse")
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError)
        .Produces(StatusCodes.Status200OK);
    }
}
