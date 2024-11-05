using ErrorOr;
using MediatR;
using Modular.Customers.Models;

namespace Modular.Customers.Create;

internal sealed record CreateCustomerCommand(string FirstName, string MiddleName, string LastName,
string Street, string City, string Zip, string State, string Email, string Phone) : IRequest<ErrorOr<CreateCustomerResponse>>
{
}

internal sealed class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, ErrorOr<CreateCustomerResponse>>
{
    private readonly CustomerDbContext _customerDbContext;
    private readonly ContactFactory _contactFactory;
    private readonly ILogger<CreateCustomerCommandHandler> _logger;

    public CreateCustomerCommandHandler(CustomerDbContext customerDbContext, ILogger<CreateCustomerCommandHandler> logger,
        ContactFactory contactFactory)
    {
        _customerDbContext = customerDbContext;
        _logger = logger;
        _contactFactory = contactFactory;
    }

    public async Task<ErrorOr<CreateCustomerResponse>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        ErrorOr<FullName> fullNameResponse = FullName.Create(request.FirstName, request.MiddleName, request.LastName);

        if (fullNameResponse.IsError)
        {
            return fullNameResponse.FirstError;
        }

        Address address = Address.Create(request.Street, request.City, request.State, request.Zip);
        ErrorOr<Contact> contactResponse = await _contactFactory.CreateAsync(request.Email, request.Phone);

        if(contactResponse.IsError)
        {
            return contactResponse.FirstError;
        }

        try
        {
            Customer customer = Customer.Create(fullNameResponse.Value, address, address, contactResponse.Value);

            await _customerDbContext.Customers.AddAsync(customer, cancellationToken);
            await _customerDbContext.SaveChangesAsync(cancellationToken);

            return new CreateCustomerResponse(customer.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Creating customer failed");
            return Error.Failure("Customer.Failure","Creating customer failed");
        }
    }
}