using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Modular.Common.User;
using Modular.Customers.Models;

namespace Modular.Customers.UseCases.Create;

internal sealed record CreateCustomerCommand(string FirstName, string? MiddleName, string LastName,
AddressDto Address, AddressDto? ShippingAddress, string? Email, string? Phone, PrimaryContactType PrimaryContactType) : IRequest<ErrorOr<CreateCustomerResponse>>
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

        Address address = Address.Create(request.Address.Street, request.Address.City, request.Address.State, request.Address.Zip);

        Address shippingAddress = request.ShippingAddress is not null ?
                Address.Create(request.ShippingAddress.Street, request.ShippingAddress.City, request.ShippingAddress.State, request.ShippingAddress.Zip)
                : address;

        ErrorOr<Contact> contactResponse = await _contactFactory.CreateAsync(request.Email, request.Phone, request.PrimaryContactType);

        if (contactResponse.IsError)
        {
            return contactResponse.FirstError;
        }

        try
        {
            Customer customer = Customer.Create(fullNameResponse.Value, address, shippingAddress, contactResponse.Value);

            await _customerDbContext.Customers.AddAsync(customer, cancellationToken);
            await _customerDbContext.SaveChangesAsync(cancellationToken);

            return new CreateCustomerResponse(customer.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Creating customer failed");
            return Error.Failure("Customer.Failure", "Creating customer failed");
        }
    }
}