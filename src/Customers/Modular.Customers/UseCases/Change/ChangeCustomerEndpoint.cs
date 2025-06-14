﻿using Carter;
using ErrorOr;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modular.Common;

namespace Modular.Customers.UseCases.Change;
public sealed class ChangeAddressEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/customers/id", async (Guid id, ChangeCustomerRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            ChangeCustomerCommand command = new(id, request.FirstName, request.MiddleName, request.LastName,
                request.Address, request.ShippingAddress, request.Email, request.Phone, request.PrimaryContactType);

            ErrorOr<Unit> response = await sender.Send(command, cancellationToken);

            return response.ToResult(_ => Results.Ok());
        })
        .WithName("ChangeCustomer")
        .WithTags("Customers")
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError)
        .Produces(StatusCodes.Status200OK);
    }
}
