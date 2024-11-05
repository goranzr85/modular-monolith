using Carter;
using Microsoft.EntityFrameworkCore;

namespace Modular.Customers.Get;
public sealed class GetCustomerEndpoint : ICarterModule
{

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/customers/id", async (Guid id, CustomerDbContext customerDbContext, CancellationToken cancellationToken) =>
        {
            GetCustomerResponse? customer = await customerDbContext.Customers
            .Where(c => c.Id == id)
            .Select(x => new GetCustomerResponse(x.Id, x.FullName.FirstName, x.FullName.MiddleName, x.FullName.LastName,
                    x.Address.Street, x.Address.City, x.Address.Zip, x.Address.State, x.Contact.Email, x.Contact.Phone))
            .FirstOrDefaultAsync(cancellationToken);

            if (customer is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(customer);
        })
        .WithName("GetCustomer")
        .WithTags("Customers")
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status200OK);

        app.MapGet("/api/customers", async (CustomerDbContext customerDbContext, CancellationToken cancellationToken) =>
        {
            GetCustomerResponse[] customers = await customerDbContext.Customers
            .Select(x => new GetCustomerResponse(x.Id, x.FullName.FirstName, x.FullName.MiddleName, x.FullName.LastName,
                    x.Address.Street, x.Address.City, x.Address.Zip, x.Address.State, x.Contact.Email, x.Contact.Phone))
            .ToArrayAsync(cancellationToken);

            return Results.Ok(customers);
        })
        .WithName("GetCustomers")
        .WithTags("Customers")
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status200OK);
    }
}
