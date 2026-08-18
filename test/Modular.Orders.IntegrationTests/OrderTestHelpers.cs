using Microsoft.Extensions.DependencyInjection;
using Modular.Common;
using Modular.Orders.UseCases.Common;

namespace Modular.Orders.IntegrationTests;

internal static class OrderTestHelpers
{
    // Each call runs in its own DI scope (its own OrderDbContext), matching the one-scope-per-request
    // lifetime the handlers are written for. Several handlers read via AsNoTracking() and later call
    // Update()/Add() on the same DbContext; reusing one scope/DbContext across multiple commands for the
    // same order trips EF Core's "entity already tracked" conflict, which isn't representative of how
    // the app actually runs.
    public static async Task<T> RunAsync<T>(OrderDatabaseFixture fixture, Func<IServiceProvider, Task<T>> action)
    {
        await using AsyncServiceScope scope = fixture.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    public static async Task RunAsync(OrderDatabaseFixture fixture, Func<IServiceProvider, Task> action)
    {
        await using AsyncServiceScope scope = fixture.Services.CreateAsyncScope();
        await action(scope.ServiceProvider);
    }

    public static async Task<int> SeedProductAsync(OrderDbContext dbContext, uint stockQuantity, decimal price = 9.99m)
    {
        Product product = Product.Create(Guid.NewGuid().ToString("N")[..10], "Test Product", "Test description.", Price.Create(price));
        product.IncreaseStock(stockQuantity);

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        return product.Id;
    }
}
