using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common.User;
using Modular.Customers.IntegrationEvents;
using Modular.Notifications.Customers;
using Xunit;
using Address = Modular.Customers.IntegrationEvents.Address;
using FullName = Modular.Customers.IntegrationEvents.FullName;

namespace Modular.Notifications.IntegrationTests;

[Collection(nameof(NotificationDatabaseCollection))]
public sealed class CustomerEventsHandlerTests
{
    private readonly NotificationDatabaseFixture _fixture;

    public CustomerEventsHandlerTests(NotificationDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Consume_CustomerCreatedEvent_PersistsNewCustomer()
    {
        await using NotificationTestApp app = await _fixture.CreateAppAsync();
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        NotificationDbContext dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        Guid customerId = Guid.NewGuid();
        CustomerCreatedEvent createdEvent = new(customerId,
            new FullName("John", null, "Doe"),
            new Address("123 Main St", "Springfield", "IL", "62704"),
            new ContactInfo("john@example.com", null, PrimaryContactType.Email));

        await app.Harness.Bus.Publish(createdEvent);
        Assert.True(await app.Harness.Consumed.Any<CustomerCreatedEvent>());

        Customer? customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId);
        Assert.NotNull(customer);
        Assert.Equal("John", customer.FullName.FirstName);
        Assert.Equal("john@example.com", customer.Contact.Email);
    }

    [Fact]
    public async Task Consume_CustomerCreatedEvent_WithAlreadyExistingId_DoesNotThrowOrDuplicate()
    {
        // Regression test: CustomerEventsHandler previously logged "already exists" but fell through
        // and tried to AddAsync a second Customer row with the same Id anyway, violating the primary key.
        await using NotificationTestApp app = await _fixture.CreateAppAsync();
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        NotificationDbContext dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        Guid customerId = Guid.NewGuid();
        CustomerCreatedEvent createdEvent = new(customerId,
            new FullName("Jane", null, "Roe"),
            new Address("456 Oak Ave", "Chicago", "IL", "60601"),
            new ContactInfo("jane@example.com", null, PrimaryContactType.Email));

        await app.Harness.Bus.Publish(createdEvent);
        Assert.True(await app.Harness.Consumed.Any<CustomerCreatedEvent>());

        await app.Harness.Bus.Publish(createdEvent);
        Assert.True(await app.Harness.Consumed.Any<CustomerCreatedEvent>(x => x.Context.Message.Id == customerId));

        int count = await dbContext.Customers.AsNoTracking().CountAsync(c => c.Id == customerId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Consume_CustomerChangedNameEvent_UpdatesExistingCustomer()
    {
        await using NotificationTestApp app = await _fixture.CreateAppAsync();
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        NotificationDbContext dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        Guid customerId = Guid.NewGuid();
        await app.Harness.Bus.Publish(new CustomerCreatedEvent(customerId,
            new FullName("John", null, "Doe"),
            new Address("123 Main St", "Springfield", "IL", "62704"),
            new ContactInfo("john@example.com", null, PrimaryContactType.Email)));
        Assert.True(await app.Harness.Consumed.Any<CustomerCreatedEvent>());

        await app.Harness.Bus.Publish(new CustomerChangedNameEvent(customerId, new FullName("Jonathan", "Q", "Doer")));
        Assert.True(await app.Harness.Consumed.Any<CustomerChangedNameEvent>());

        Customer? customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId);
        Assert.NotNull(customer);
        Assert.Equal("Jonathan", customer.FullName.FirstName);
        Assert.Equal("Doer", customer.FullName.LastName);
    }

    [Fact]
    public async Task Consume_CustomerChangedNameEvent_ForUnknownCustomer_DoesNotThrow()
    {
        // Regression test: previously fell through the null guard (no return) and dereferenced a null
        // Customer with the null-forgiving operator, throwing a NullReferenceException at runtime.
        await using NotificationTestApp app = await _fixture.CreateAppAsync();

        await app.Harness.Bus.Publish(new CustomerChangedNameEvent(Guid.NewGuid(), new FullName("Ghost", null, "Customer")));

        Assert.True(await app.Harness.Consumed.Any<CustomerChangedNameEvent>());
        Assert.False(await app.Harness.Published.Any<Fault<CustomerChangedNameEvent>>());
    }

    [Fact]
    public async Task Consume_CustomerChangedContactInformationEvent_UpdatesExistingCustomer()
    {
        await using NotificationTestApp app = await _fixture.CreateAppAsync();
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        NotificationDbContext dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        Guid customerId = Guid.NewGuid();
        await app.Harness.Bus.Publish(new CustomerCreatedEvent(customerId,
            new FullName("John", null, "Doe"),
            new Address("123 Main St", "Springfield", "IL", "62704"),
            new ContactInfo("john@example.com", null, PrimaryContactType.Email)));
        Assert.True(await app.Harness.Consumed.Any<CustomerCreatedEvent>());

        await app.Harness.Bus.Publish(new CustomerChangedContactInformationEvent(customerId,
            new ContactInfo(null, "+15551234567", PrimaryContactType.Phone)));
        Assert.True(await app.Harness.Consumed.Any<CustomerChangedContactInformationEvent>());

        Customer? customer = await dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId);
        Assert.NotNull(customer);
        Assert.Equal("+15551234567", customer.Contact.Phone);
        Assert.Equal(PrimaryContactType.Phone, customer.Contact.PrimaryContactType);
    }

    [Fact]
    public async Task Consume_CustomerChangedContactInformationEvent_ForUnknownCustomer_DoesNotThrow()
    {
        // Regression test: same missing-return bug as the name-change handler.
        await using NotificationTestApp app = await _fixture.CreateAppAsync();

        await app.Harness.Bus.Publish(new CustomerChangedContactInformationEvent(Guid.NewGuid(),
            new ContactInfo("ghost@example.com", null, PrimaryContactType.Email)));

        Assert.True(await app.Harness.Consumed.Any<CustomerChangedContactInformationEvent>());
        Assert.False(await app.Harness.Published.Any<Fault<CustomerChangedContactInformationEvent>>());
    }
}
