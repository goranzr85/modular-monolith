using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Modular.Common;
using Modular.Common.User;
using Modular.Customers.Models;

namespace Modular.Customers.UseCases.Create;

internal sealed record CreateCustomerCommand(string FirstName, string? MiddleName, string LastName,
AddressDto Address, AddressDto? ShippingAddress, string? Email, string? Phone, PrimaryContactType PrimaryContactType)
{
}

internal sealed class CreateCustomerCommandHandler
{
    private readonly CustomerDbContext _customerDbContext;
    private readonly ContactFactory _contactFactory;
    private readonly ILogger<CreateCustomerCommandHandler> _logger;
    private readonly IValidator<CreateCustomerCommand> _validator;

    public CreateCustomerCommandHandler(CustomerDbContext customerDbContext, ILogger<CreateCustomerCommandHandler> logger,
        ContactFactory contactFactory, IValidator<CreateCustomerCommand> validator)
    {
        _customerDbContext = customerDbContext;
        _logger = logger;
        _contactFactory = contactFactory;
        _validator = validator;
    }

    public async Task<ErrorOr<CreateCustomerResponse>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        List<Error> validationErrors = await _validator.GetValidationErrorsAsync(request, cancellationToken);

        if (validationErrors.Count > 0)
        {
            return validationErrors;
        }

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