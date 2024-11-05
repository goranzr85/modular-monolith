using Carter;
using ErrorOr;
using MediatR;

namespace Modular.Customers.Delete;
public sealed class DeleteCustomerEndpoint : ICarterModule
{

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/customers/id", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            DeleteCustomerCommand command = new(id);

            ErrorOr<Unit> response = await sender.Send(command, cancellationToken);

            if (response.IsError)
            {
                return response.FirstError.Type switch
                {
                    ErrorType.Validation => Results.BadRequest(response.FirstError),
                    ErrorType.Failure => Results.Problem(statusCode: 500, detail: response.FirstError.Description),
                    ErrorType.NotFound => Results.NotFound(response.FirstError),
                    _ => Results.Problem(statusCode: 500, detail: "An unexpected error occurred.")
                };
            }

            return Results.Ok();
        })
        .WithName("DeleteCustomer")
        .WithTags("Customers")
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status200OK);
    }
}
