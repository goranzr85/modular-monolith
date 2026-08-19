using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common;
using Modular.Common.User;
using Modular.Customers.Models;
using Modular.Customers.UseCases.Create;
using Xunit;

namespace Modular.Customers.IntegrationTests;

[Collection(nameof(CustomerDatabaseCollection))]
public sealed class CreateCustomerCommandTests
{
    private readonly CustomerDatabaseFixture _fixture;

    public CreateCustomerCommandTests(CustomerDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..10];

    private static AddressDto ValidAddress() => new("123 Main St", "Springfield", "62704", "IL");

    private static CreateCustomerCommand ValidCommand(
        string? middleName = null,
        AddressDto? address = null,
        AddressDto? shippingAddress = null,
        string? email = null,
        string? phone = null,
        PrimaryContactType primaryContactType = PrimaryContactType.Email) =>
        new("John", middleName, "Doe", address ?? ValidAddress(), shippingAddress,
            email ?? $"{Unique()}@example.com", phone, primaryContactType);

    [Fact]
    public async Task Handle_WithEmailOnlyAndNoShippingAddress_PersistsCustomerWithShippingAddressDefaultedToAddress()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        CustomerDbContext dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

        string email = $"{Unique()}@example.com";
        CreateCustomerCommand command = ValidCommand(email: email, primaryContactType: PrimaryContactType.Email);

        ErrorOr<CreateCustomerResponse> result = await sender.Send(command);

        Assert.False(result.IsError);

        Customer? customer = await dbContext.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == result.Value.CustomerId);

        Assert.NotNull(customer);
        Assert.Equal("John", customer.FullName.FirstName);
        Assert.Equal("Doe", customer.FullName.LastName);
        Assert.Equal(email, customer.Contact.Email);
        Assert.True(customer.Address.Equals(customer.ShippingAddress));

        OutboxMessage? outboxMessage = await dbContext.OutboxMessages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Type == "CustomerCreatedEvent" && m.Content.Contains(email));
        Assert.NotNull(outboxMessage);
    }

    [Fact]
    public async Task Handle_WithDistinctShippingAddress_PersistsShippingAddressSeparately()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        CustomerDbContext dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

        AddressDto shipping = new("456 Oak Ave", "Chicago", "60601", "IL");
        CreateCustomerCommand command = ValidCommand(shippingAddress: shipping);

        ErrorOr<CreateCustomerResponse> result = await sender.Send(command);

        Assert.False(result.IsError);

        Customer? customer = await dbContext.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == result.Value.CustomerId);

        Assert.NotNull(customer);
        Assert.False(customer.Address.Equals(customer.ShippingAddress));
        Assert.Equal("Chicago", customer.ShippingAddress!.City);
    }

    [Fact]
    public async Task Handle_WithPhoneOnlyAsPrimaryContact_Succeeds()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        string phone = $"+1555{Unique()[..7]}";
        CreateCustomerCommand command = new("Jane", null, "Roe", ValidAddress(), null,
            null, phone, PrimaryContactType.Phone);

        ErrorOr<CreateCustomerResponse> result = await sender.Send(command);

        Assert.False(result.IsError);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Handle_WithMissingFirstName_ReturnsValidationError(string? firstName)
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        CreateCustomerCommand command = new(firstName!, null, "Doe", ValidAddress(), null,
            $"{Unique()}@example.com", null, PrimaryContactType.Email);

        ErrorOr<CreateCustomerResponse> result = await sender.Send(command);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_WithFirstNameExceedingMaxLength_ReturnsValidationError()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        CreateCustomerCommand command = new(new string('a', 51), null, "Doe", ValidAddress(), null,
            $"{Unique()}@example.com", null, PrimaryContactType.Email);

        ErrorOr<CreateCustomerResponse> result = await sender.Send(command);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_WithoutMiddleName_Succeeds()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        CreateCustomerCommand command = ValidCommand(middleName: null);

        ErrorOr<CreateCustomerResponse> result = await sender.Send(command);

        Assert.False(result.IsError);
    }

    [Fact]
    public async Task Handle_WithMiddleNameExceedingMaxLength_ReturnsValidationError()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        CreateCustomerCommand command = ValidCommand(middleName: new string('m', 51));

        ErrorOr<CreateCustomerResponse> result = await sender.Send(command);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_WithEmptyAddressStreet_ReturnsValidationError()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        AddressDto invalidAddress = new("", "Springfield", "62704", "IL");
        CreateCustomerCommand command = ValidCommand(address: invalidAddress);

        ErrorOr<CreateCustomerResponse> result = await sender.Send(command);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_WithNeitherEmailNorPhone_ReturnsContactValidationError()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        CreateCustomerCommand command = new("John", null, "Doe", ValidAddress(), null,
            null, null, PrimaryContactType.Email);

        ErrorOr<CreateCustomerResponse> result = await sender.Send(command);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
        Assert.Equal("Customer.Contact.Validation", result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_WithPrimaryContactTypeEmailButNoEmail_ReturnsContactValidationError()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        string phone = $"+1555{Unique()[..7]}";
        CreateCustomerCommand command = new("John", null, "Doe", ValidAddress(), null,
            null, phone, PrimaryContactType.Email);

        ErrorOr<CreateCustomerResponse> result = await sender.Send(command);

        Assert.True(result.IsError);
        Assert.Equal("Customer.Contact.Validation", result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_WithPrimaryContactTypePhoneButNoPhone_ReturnsContactValidationError()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        CreateCustomerCommand command = ValidCommand(primaryContactType: PrimaryContactType.Phone);

        ErrorOr<CreateCustomerResponse> result = await sender.Send(command);

        Assert.True(result.IsError);
        Assert.Equal("Customer.Contact.Validation", result.FirstError.Code);
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ReturnsValidationErrorAndDoesNotCreateSecondCustomer()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        CustomerDbContext dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();

        string email = $"{Unique()}@example.com";
        ErrorOr<CreateCustomerResponse> first = await sender.Send(ValidCommand(email: email));
        Assert.False(first.IsError);

        ErrorOr<CreateCustomerResponse> second = await sender.Send(ValidCommand(email: email));

        Assert.True(second.IsError);
        Assert.Equal(ErrorType.Validation, second.FirstError.Type);
        Assert.Equal("Customer.Contact.Validation", second.FirstError.Code);

        int count = await dbContext.Customers.AsNoTracking().CountAsync(c => c.Contact.Email == email);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Handle_WithDuplicatePhone_ReturnsValidationError()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        string phone = $"+1555{Unique()[..7]}";
        CreateCustomerCommand first = new("John", null, "Doe", ValidAddress(), null, null, phone, PrimaryContactType.Phone);
        ErrorOr<CreateCustomerResponse> firstResult = await sender.Send(first);
        Assert.False(firstResult.IsError);

        CreateCustomerCommand second = new("Jane", null, "Roe", ValidAddress(), null, null, phone, PrimaryContactType.Phone);
        ErrorOr<CreateCustomerResponse> secondResult = await sender.Send(second);

        Assert.True(secondResult.IsError);
        Assert.Equal("Customer.Contact.Validation", secondResult.FirstError.Code);
    }

    [Fact]
    public async Task Handle_WithInvalidEmailFormat_ReturnsValidationError()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        CreateCustomerCommand command = ValidCommand(email: "not-an-email");

        ErrorOr<CreateCustomerResponse> result = await sender.Send(command);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_WithEmailExceedingMaxLength_ReturnsValidationError()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        string longEmail = new string('a', 75) + "@example.com";
        CreateCustomerCommand command = ValidCommand(email: longEmail);

        ErrorOr<CreateCustomerResponse> result = await sender.Send(command);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task Handle_WithPhoneExceedingMaxLengthAndNoEmail_ReturnsValidationError()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        string longPhone = new string('5', 51);
        CreateCustomerCommand command = new("John", null, "Doe", ValidAddress(), null,
            null, longPhone, PrimaryContactType.Phone);

        ErrorOr<CreateCustomerResponse> result = await sender.Send(command);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }
}
