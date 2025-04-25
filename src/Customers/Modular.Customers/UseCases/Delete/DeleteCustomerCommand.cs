using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Modular.Customers.Models;

namespace Modular.Customers.UseCases.Delete;

internal sealed record DeleteCustomerCommand(Guid CustomerId) : IRequest<ErrorOr<Unit>>
{
}

internal sealed class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, ErrorOr<Unit>>
{
    private readonly CustomerDbContext _customerDbContext;

    public DeleteCustomerCommandHandler(CustomerDbContext customerDbContext)
    {
        _customerDbContext = customerDbContext;
    }

    public async Task<ErrorOr<Unit>> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        Customer? customer = await _customerDbContext.Customers
             .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer is null)
        {
            return Error.NotFound("Customers.NotFound", "Customer does not exist.");
        }

        _customerDbContext.Customers.Remove(customer);
        await _customerDbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}