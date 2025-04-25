using Carter;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modular.Common;

namespace Modular.Orders.UseCases.Orders.Change.AddProducts;
public sealed class AddProductToOrderEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/orders/add/{orderId:guid}", async (Guid orderId, AddProductToOrderRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            AddProductCommand command = new(orderId, request.ProductId, request.Quantity, request.Price);

            ErrorOr<Unit> response = await sender.Send(command, cancellationToken);

            return response.ToResult((orderId) => Results.Created($"/api/orders/{orderId}", orderId));
        })
        .WithName("AddProduct")
        .WithTags("Orders")
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError)
        .Produces(StatusCodes.Status200OK);
    }
}
