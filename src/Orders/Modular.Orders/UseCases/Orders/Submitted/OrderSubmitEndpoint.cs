using Carter;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modular.Authorization;
using Modular.Common;
using Modular.Orders.Authorization;
using Modular.Orders.UseCases.Common;

namespace Modular.Orders.UseCases.Orders.Submitted;

public sealed class OrderSubmitEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/submit/{orderId:guid}", async (Guid orderId, ISender sender, CancellationToken cancellationToken) =>
        {
            OrderSubmitCommand command = new(orderId);

            ErrorOr<Unit> response = await sender.Send(command, cancellationToken);

            return response.ToResult(_ => Results.NoContent());
        })
       .WithName("SubmitOrder")
       .WithTags(Constants.EndpointTag)
       .Produces(StatusCodes.Status400BadRequest)
       .Produces(StatusCodes.Status500InternalServerError)
       .Produces(StatusCodes.Status200OK)
       .RequireAuthorization(policy => policy.RequirePermission(Permissions.OrderCreate));
    }
}
