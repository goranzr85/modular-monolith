using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common;
using Modular.Common.User;
using Modular.Customers.Models;
using Modular.Customers.UseCases.Change;
using Modular.Customers.UseCases.Create;
using Xunit;

namespace Modular.Customers.IntegrationTests;

[Collection(nameof(CustomerDatabaseCollection))]
public sealed class ChangeCustomerCommandTests
{
    private readonly CustomerDatabaseFixture _fixture;

    public ChangeCustomerCommandTests(CustomerDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..10];

    private static AddressDto ValidAddress() => new("123 Main St", "Springfield", "62704", "IL");

    private static async Task<(Guid CustomerId, string Email)> SeedCustomerAsync(CreateCustomerCommandHandler handler, AddressDto? shippingAddress = null)
    {
        string email = $"{Unique()}@example.com";
        CreateCustomerCommand createCommand = new("John", null, "Doe", ValidAddress(), shippingAddress,
            email, null, PrimaryContactType.Email);

        ErrorOr<CreateCustomerResponse> result = await handler.Handle(createCommand, CancellationToken.None);
        Assert.False(result.IsError);

        return (result.Value.CustomerId, email);
    }

    [Fact]
    public async Task Handle_WithNewAddress_PersistsUpdatedAddress()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        CreateCustomerCommandHandler createHandler = scope.ServiceProvider.GetRequiredService<CreateCustomerCommandHandler>();
        ChangeCustomerCommandHandler handler = scope.ServiceProvider.GetRequiredService<ChangeCustomerCommandHandler>();
        CustomerDbContext dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

        (Guid customerId, string email) = await SeedCustomerAsync(createHandler);

        AddressDto newAddress = new("789 Elm St", "Denver", "80202", "CO");
        ChangeCustomerCommand command = new(customerId, "John", null, "Doe", newAddress, null, email, null, PrimaryContactType.Email);

        ErrorOr<Unit> result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsError);

        Customer? customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId);

        Assert.NotNull(customer);
        Assert.Equal("Denver", customer.Address.City);
        Assert.Equal("789 Elm St", customer.Address.Street);
    }

    [Fact]
    public async Task Handle_WithSameAddressAsCurrent_SucceedsWithoutChangingIt()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        CreateCustomerCommandHandler createHandler = scope.ServiceProvider.GetRequiredService<CreateCustomerCommandHandler>();
        ChangeCustomerCommandHandler handler = scope.ServiceProvider.GetRequiredService<ChangeCustomerCommandHandler>();
        CustomerDbContext dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

        (Guid customerId, string email) = await SeedCustomerAsync(createHandler);

        ChangeCustomerCommand command = new(customerId, "John", null, "Doe", ValidAddress(), null, email, null, PrimaryContactType.Email);

        ErrorOr<Unit> result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsError);

        Customer? customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId);
        Assert.NotNull(customer);
        Assert.Equal("Springfield", customer.Address.City);
    }

    [Fact]
    public async Task Handle_WithNewShippingAddress_PersistsAndRaisesShippingAddressChangedEvent()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        CreateCustomerCommandHandler createHandler = scope.ServiceProvider.GetRequiredService<CreateCustomerCommandHandler>();
        ChangeCustomerCommandHandler handler = scope.ServiceProvider.GetRequiredService<ChangeCustomerCommandHandler>();
        CustomerDbContext dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

        (Guid customerId, string email) = await SeedCustomerAsync(createHandler);

        AddressDto newShipping = new("999 Pine Rd", "Austin", "73301", "TX");
        ChangeCustomerCommand command = new(customerId, "John", null, "Doe", ValidAddress(), newShipping, email, null, PrimaryContactType.Email);

        ErrorOr<Unit> result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsError);

        Customer? customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId);
        Assert.NotNull(customer);
        Assert.Equal("Austin", customer.ShippingAddress!.City);

        bool eventRaised = await dbContext.OutboxMessages.AsNoTracking()
            .AnyAsync(m => m.Type == "CustomerChangedShippingAddressEvent" && m.Content.Contains(customerId.ToString()));
        Assert.True(eventRaised);
    }

    [Fact]
    public async Task Handle_WithSameShippingAddressAsCurrent_DoesNotRaiseShippingAddressChangedEvent()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        CreateCustomerCommandHandler createHandler = scope.ServiceProvider.GetRequiredService<CreateCustomerCommandHandler>();
        ChangeCustomerCommandHandler handler = scope.ServiceProvider.GetRequiredService<ChangeCustomerCommandHandler>();
        CustomerDbContext dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

        (Guid customerId, string email) = await SeedCustomerAsync(createHandler);

        ChangeCustomerCommand command = new(customerId, "John", null, "Doe", ValidAddress(), ValidAddress(), email, null, PrimaryContactType.Email);

        ErrorOr<Unit> result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsError);

        bool eventRaised = await dbContext.OutboxMessages.AsNoTracking()
            .AnyAsync(m => m.Type == "CustomerChangedShippingAddressEvent" && m.Content.Contains(customerId.ToString()));
        Assert.False(eventRaised);
    }

    [Fact]
    public async Task Handle_WithNewName_PersistsUpdatedNameAndRaisesEvent()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        CreateCustomerCommandHandler createHandler = scope.ServiceProvider.GetRequiredService<CreateCustomerCommandHandler>();
        ChangeCustomerCommandHandler handler = scope.ServiceProvider.GetRequiredService<ChangeCustomerCommandHandler>();
        CustomerDbContext dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

        (Guid customerId, string email) = await SeedCustomerAsync(createHandler);

        ChangeCustomerCommand command = new(customerId, "Jonathan", "Q", "Doer", ValidAddress(), null, email, null, PrimaryContactType.Email);

        ErrorOr<Unit> result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsError);

        Customer? customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId);
        Assert.NotNull(customer);
        Assert.Equal("Jonathan", customer.FullName.FirstName);
        Assert.Equal("Doer", customer.FullName.LastName);
        Assert.Equal("Q", customer.FullName.MiddleName);

        bool eventRaised = await dbContext.OutboxMessages.AsNoTracking()
            .AnyAsync(m => m.Type == "CustomerChangedNameEvent" && m.Content.Contains(customerId.ToString()));
        Assert.True(eventRaised);
    }

    [Fact]
    public async Task Handle_WithNewEmail_PersistsUpdatedContactAndRaisesEvent()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        CreateCustomerCommandHandler createHandler = scope.ServiceProvider.GetRequiredService<CreateCustomerCommandHandler>();
        ChangeCustomerCommandHandler handler = scope.ServiceProvider.GetRequiredService<ChangeCustomerCommandHandler>();
        CustomerDbContext dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

        (Guid customerId, _) = await SeedCustomerAsync(createHandler);

        string newEmail = $"{Unique()}@example.com";
        ChangeCustomerCommand command = new(customerId, "John", null, "Doe", ValidAddress(), null, newEmail, null, PrimaryContactType.Email);

        ErrorOr<Unit> result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsError);

        Customer? customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId);
        Assert.NotNull(customer);
        Assert.Equal(newEmail, customer.Contact.Email);

        bool eventRaised = await dbContext.OutboxMessages.AsNoTracking()
            .AnyAsync(m => m.Type == "CustomerChangedContactInformationEvent" && m.Content.Contains(customerId.ToString()));
        Assert.True(eventRaised);
    }

    [Fact]
    public async Task Handle_WithEmailAlreadyUsedByAnotherCustomer_ReturnsValidationErrorAndDoesNotChangeContact()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        CreateCustomerCommandHandler createHandler = scope.ServiceProvider.GetRequiredService<CreateCustomerCommandHandler>();
        ChangeCustomerCommandHandler handler = scope.ServiceProvider.GetRequiredService<ChangeCustomerCommandHandler>();
        CustomerDbContext dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

        (_, string otherEmail) = await SeedCustomerAsync(createHandler);
        (Guid customerId, string originalEmail) = await SeedCustomerAsync(createHandler);

        ChangeCustomerCommand command = new(customerId, "John", null, "Doe", ValidAddress(), null, otherEmail, null, PrimaryContactType.Email);

        ErrorOr<Unit> result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal("Customer.Contact.Validation", result.FirstError.Code);

        Customer? customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId);
        Assert.NotNull(customer);
        Assert.Equal(originalEmail, customer.Contact.Email);
    }

    [Fact]
    public async Task Handle_WithUnknownCustomerId_ReturnsNotFound()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ChangeCustomerCommandHandler handler = scope.ServiceProvider.GetRequiredService<ChangeCustomerCommandHandler>();

        ChangeCustomerCommand command = new(Guid.NewGuid(), "John", null, "Doe", ValidAddress(), null,
            $"{Unique()}@example.com", null, PrimaryContactType.Email);

        ErrorOr<Unit> result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
        Assert.Equal("Customers.NotFound", result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_WithInvalidData_ReturnsValidationErrorAndDoesNotChangeCustomer()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        CreateCustomerCommandHandler createHandler = scope.ServiceProvider.GetRequiredService<CreateCustomerCommandHandler>();
        ChangeCustomerCommandHandler handler = scope.ServiceProvider.GetRequiredService<ChangeCustomerCommandHandler>();
        CustomerDbContext dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

        (Guid customerId, string email) = await SeedCustomerAsync(createHandler);

        ChangeCustomerCommand command = new(customerId, "", null, "Doe", ValidAddress(), null, email, null, PrimaryContactType.Email);

        ErrorOr<Unit> result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);

        Customer? customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId);
        Assert.NotNull(customer);
        Assert.Equal("John", customer.FullName.FirstName);
    }

    [Fact]
    public async Task Handle_WithNoActualChanges_Succeeds()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        CreateCustomerCommandHandler createHandler = scope.ServiceProvider.GetRequiredService<CreateCustomerCommandHandler>();
        ChangeCustomerCommandHandler handler = scope.ServiceProvider.GetRequiredService<ChangeCustomerCommandHandler>();

        (Guid customerId, string email) = await SeedCustomerAsync(createHandler);

        ChangeCustomerCommand command = new(customerId, "John", null, "Doe", ValidAddress(), null, email, null, PrimaryContactType.Email);

        ErrorOr<Unit> result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsError);
    }
}
