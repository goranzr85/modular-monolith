using Carter;
using ErrorOr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modular.Authorization;
using Modular.Common;
using Modular.Orders.Authorization;
using Modular.Orders.UseCases.Common;

namespace Modular.Orders.UseCases.Orders.Change.ChangeProductQuantity.Increase;
public sealed class IncreaseProductQuantityEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/increase-quantity/{orderId:guid}", async (Guid orderId, IncreaseProductQuantityRequest request, IncreaseProductQuantityCommandHandler handler, CancellationToken cancellationToken) =>
        {
            IncreaseProductQuantityCommand command = new(orderId, request.ProductId, request.Quantity);

            ErrorOr<Unit> response = await handler.Handle(command, cancellationToken);

            return response.ToResult(_ => Results.NoContent());
        })
        .WithName("IncreaseProductQuantity")
        .WithTags(Constants.EndpointTag)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError)
        .Produces(StatusCodes.Status200OK)
        .RequireAuthorization(policy => policy.RequirePermission(Permissions.OrderUpdate));
    }
}
