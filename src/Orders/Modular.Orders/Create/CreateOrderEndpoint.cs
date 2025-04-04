using Carter;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modular.Common;

namespace Modular.Orders.Create;

public sealed class CreateOrderEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/create", async (CreateOrderRequest createOrderRequest, ISender sender, TimeProvider dateTimeProvider, CancellationToken cancellationToken) =>
        {
            Guid orderId = Ulid.NewUlid().ToGuid();
            CreateOrderCommand command = new(orderId, OrderDate: dateTimeProvider.GetUtcNow(), createOrderRequest.CustomerId, createOrderRequest.Items);

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
