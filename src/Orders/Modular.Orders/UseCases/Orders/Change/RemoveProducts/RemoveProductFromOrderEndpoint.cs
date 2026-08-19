using Carter;
using ErrorOr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modular.Authorization;
using Modular.Common;
using Modular.Orders.Authorization;
using Modular.Orders.UseCases.Common;

namespace Modular.Orders.UseCases.Orders.Change.RemoveProducts;
public sealed class RemoveProductFromOrderEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/remove/{orderId:guid}", async (Guid orderId, RemoveProductFromOrderRequest request, RemoveProductCommandHandler handler, CancellationToken cancellationToken) =>
        {
            RemoveProductCommand command = new(orderId, request.ProductId);

            ErrorOr<Unit> response = await handler.Handle(command, cancellationToken);

            return response.ToResult(_ => Results.Ok());
        })
        .WithName("RemoveProduct")
        .WithTags(Constants.EndpointTag)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError)
        .Produces(StatusCodes.Status200OK)
        .RequireAuthorization(policy => policy.RequirePermission(Permissions.OrderUpdate));
    }
}
