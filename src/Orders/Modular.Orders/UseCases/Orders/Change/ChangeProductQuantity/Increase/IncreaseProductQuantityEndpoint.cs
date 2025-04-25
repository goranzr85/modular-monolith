using Carter;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modular.Common;
using Modular.Orders.UseCases.Orders.Change.ChangeProductQuantity.Decrease;

namespace Modular.Orders.UseCases.Orders.Change.ChangeProductQuantity.Increase;
public sealed class IncreaseProductQuantityEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/increase-quantity/{orderId:guid}", async (Guid orderId, IncreaseProductQuantityRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            DecreaseProductQuantityCommand command = new(orderId, request.ProductId, request.Quantity);

            ErrorOr<Unit> response = await sender.Send(command, cancellationToken);

            return response.ToResult(_ => Results.NoContent());
        })
        .WithName("IncreaseProductQuantity")
        .WithTags("Orders")
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError)
        .Produces(StatusCodes.Status200OK);
    }
}
