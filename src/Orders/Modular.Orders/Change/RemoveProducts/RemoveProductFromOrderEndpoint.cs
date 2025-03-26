using Carter;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modular.Common;

namespace Modular.Orders.Change.RemoveProducts;
public sealed class RemoveProductFromOrderEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/{orderId: guid}/remove", async (Guid orderId, RemoveProductFromOrderRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            RemoveProductCommand command = new(orderId, request.ProductId);

            ErrorOr<Unit> response = await sender.Send(command, cancellationToken);

            return response.ToResult(_ => Results.Ok());
        })
        .WithName("RemoveProduct")
        .WithTags("Orders")
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError)
        .Produces(StatusCodes.Status200OK);
    }
}
