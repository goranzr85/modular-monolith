using Carter;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modular.Common;

namespace Modular.Customers.Create;
public sealed class ChangeAddressEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/customers", async (CreateCustomerRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            CreateCustomerCommand command = new(request.FirstName, request.MiddleName, request.LastName,
                request.Address, request.ShippingAddress, request.Email, request.Phone);

            ErrorOr<CreateCustomerResponse> response = await sender.Send(command, cancellationToken);

            return response.ToResult((customerId) => Results.Created($"/api/customers/{customerId}", customerId));
        })
      .WithName("CreateCustomer")
      .WithTags("Customers")
      .Produces(StatusCodes.Status400BadRequest)
      .Produces(StatusCodes.Status500InternalServerError)
      .Produces(StatusCodes.Status201Created);
    }
}
