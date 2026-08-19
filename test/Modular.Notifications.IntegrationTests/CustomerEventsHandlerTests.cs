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

        await app.Publisher.PublishAsync(createdEvent);

        Customer? customer = await Eventually.WaitForAsync(() =>
            dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId));
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

        await app.Publisher.PublishAsync(createdEvent);
        Customer? customer = await Eventually.WaitForAsync(() =>
            dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId));
        Assert.NotNull(customer);

        await app.Publisher.PublishAsync(createdEvent);
        await Task.Delay(TimeSpan.FromSeconds(1));

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
        await app.Publisher.PublishAsync(new CustomerCreatedEvent(customerId,
            new FullName("John", null, "Doe"),
            new Address("123 Main St", "Springfield", "IL", "62704"),
            new ContactInfo("john@example.com", null, PrimaryContactType.Email)));
        Customer? created = await Eventually.WaitForAsync(() =>
            dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId));
        Assert.NotNull(created);

        await app.Publisher.PublishAsync(new CustomerChangedNameEvent(customerId, new FullName("Jonathan", "Q", "Doer")));

        Customer? customer = await Eventually.WaitForAsync(() =>
            dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId && c.FullName.FirstName == "Jonathan"));
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

        await app.Publisher.PublishAsync(new CustomerChangedNameEvent(Guid.NewGuid(), new FullName("Ghost", null, "Customer")));

        // A failing consumer exhausts retries and dead-letters the message; a successful one never does.
        await Task.Delay(TimeSpan.FromSeconds(2));
        Assert.False(await DeadLetterQueueHasMessageAsync<CustomerEventsHandler>(app.Connection));
    }

    [Fact]
    public async Task Consume_CustomerChangedContactInformationEvent_UpdatesExistingCustomer()
    {
        await using NotificationTestApp app = await _fixture.CreateAppAsync();
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        NotificationDbContext dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        Guid customerId = Guid.NewGuid();
        await app.Publisher.PublishAsync(new CustomerCreatedEvent(customerId,
            new FullName("John", null, "Doe"),
            new Address("123 Main St", "Springfield", "IL", "62704"),
            new ContactInfo("john@example.com", null, PrimaryContactType.Email)));
        Customer? created = await Eventually.WaitForAsync(() =>
            dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId));
        Assert.NotNull(created);

        await app.Publisher.PublishAsync(new CustomerChangedContactInformationEvent(customerId,
            new ContactInfo(null, "+15551234567", PrimaryContactType.Phone)));

        Customer? customer = await Eventually.WaitForAsync(() =>
            dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId && c.Contact.Phone == "+15551234567"));
        Assert.NotNull(customer);
        Assert.Equal("+15551234567", customer.Contact.Phone);
        Assert.Equal(PrimaryContactType.Phone, customer.Contact.PrimaryContactType);
    }

    [Fact]
    public async Task Consume_CustomerChangedContactInformationEvent_ForUnknownCustomer_DoesNotThrow()
    {
        // Regression test: same missing-return bug as the name-change handler.
        await using NotificationTestApp app = await _fixture.CreateAppAsync();

        await app.Publisher.PublishAsync(new CustomerChangedContactInformationEvent(Guid.NewGuid(),
            new ContactInfo("ghost@example.com", null, PrimaryContactType.Email)));

        // A failing consumer exhausts retries and dead-letters the message; a successful one never does.
        await Task.Delay(TimeSpan.FromSeconds(2));
        Assert.False(await DeadLetterQueueHasMessageAsync<CustomerEventsHandler>(app.Connection));
    }

    private static async Task<bool> DeadLetterQueueHasMessageAsync<TConsumer>(RabbitMQ.Client.IConnection connection)
    {
        string deadLetterQueue = $"{Modular.Common.Messaging.RabbitMqIntegrationEventNaming.QueueFor(typeof(TConsumer))}.dlq";

        await using RabbitMQ.Client.IChannel channel = await connection.CreateChannelAsync();
        RabbitMQ.Client.BasicGetResult? result = await channel.BasicGetAsync(deadLetterQueue, autoAck: false);

        return result is not null;
    }
}
