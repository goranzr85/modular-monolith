using ErrorOr;
using MassTransit.Testing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modular.Common;
using Modular.Common.User;
using Modular.Customers.Infrastructure.BackgroundJobs;
using Modular.Customers.IntegrationEvents;
using Modular.Customers.Models;
using Modular.Customers.UseCases.Change;
using Modular.Customers.UseCases.Create;
using Xunit;

namespace Modular.Customers.IntegrationTests;

[Collection(nameof(OutboxJobDatabaseCollection))]
public sealed class ProcessOutboxMessagesJobTests
{
    private readonly CustomerDatabaseFixture _fixture;

    public ProcessOutboxMessagesJobTests(CustomerDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..10];

    private static AddressDto ValidAddress() => new("123 Main St", "Springfield", "62704", "IL");

    [Fact]
    public async Task Execute_WithPendingCustomerCreatedOutboxMessage_PublishesEventAndMarksMessageProcessed()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        CustomerDbContext dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        ITestHarness harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();

        string email = $"{Unique()}@example.com";
        CreateCustomerCommand createCommand = new("John", null, "Doe", ValidAddress(), null, email, null, PrimaryContactType.Email);
        ErrorOr<CreateCustomerResponse> createResult = await sender.Send(createCommand);
        Assert.False(createResult.IsError);
        Guid customerId = createResult.Value.CustomerId;

        ProcessOutboxMessagesJob job = ActivatorUtilities.CreateInstance<ProcessOutboxMessagesJob>(scope.ServiceProvider);
        await job.Execute(null!);

        OutboxMessage? outboxMessage = await dbContext.OutboxMessages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Type == "CustomerCreatedEvent" && m.Content.Contains(customerId.ToString()));

        Assert.NotNull(outboxMessage);
        Assert.NotNull(outboxMessage.ProcessedOnUtc);

        Assert.True(await harness.Published.Any<CustomerCreatedEvent>(e => e.Context.Message.Id == customerId));
    }

    [Fact]
    public async Task Execute_WithPendingShippingAddressChangedOutboxMessage_PublishesEvent()
    {
        await using AsyncServiceScope scope = _fixture.Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        CustomerDbContext dbContext = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        ITestHarness harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();

        string email = $"{Unique()}@example.com";
        CreateCustomerCommand createCommand = new("Jane", null, "Roe", ValidAddress(), null, email, null, PrimaryContactType.Email);
        ErrorOr<CreateCustomerResponse> createResult = await sender.Send(createCommand);
        Assert.False(createResult.IsError);
        Guid customerId = createResult.Value.CustomerId;

        ProcessOutboxMessagesJob job = ActivatorUtilities.CreateInstance<ProcessOutboxMessagesJob>(scope.ServiceProvider);
        await job.Execute(null!);

        AddressDto newShipping = new("999 Pine Rd", "Austin", "73301", "TX");
        ChangeCustomerCommand changeCommand = new(customerId, "Jane", null, "Roe", ValidAddress(), newShipping, email, null, PrimaryContactType.Email);
        ErrorOr<Unit> changeResult = await sender.Send(changeCommand);
        Assert.False(changeResult.IsError);

        await job.Execute(null!);

        bool processed = await dbContext.OutboxMessages.AsNoTracking()
            .AnyAsync(m => m.Type == "CustomerChangedShippingAddressEvent"
                && m.Content.Contains(customerId.ToString())
                && m.ProcessedOnUtc != null);
        Assert.True(processed);

        Assert.True(await harness.Published.Any<CustomerChangedShippingAddressEvent>(e => e.Context.Message.CustomerId == customerId));
    }
}
