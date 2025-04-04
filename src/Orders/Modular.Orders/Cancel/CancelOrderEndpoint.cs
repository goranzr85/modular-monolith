using Carter;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modular.Common;

namespace Modular.Orders.Cancel;
public sealed class CancelOrderEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/cancel/{orderId:guid}", async (Guid orderId, ISender sender, CancellationToken cancellationToken) =>
        {
            CancelOrderCommand command = new(orderId);

            ErrorOr<Unit> response = await sender.Send(command, cancellationToken);

            return response.ToResult(_ => Results.NoContent());
        })
       .WithName("CancelOrder")
       .WithTags("Orders")
       .Produces(StatusCodes.Status400BadRequest)
       .Produces(StatusCodes.Status500InternalServerError)
       .Produces(StatusCodes.Status200OK);
    }
}
