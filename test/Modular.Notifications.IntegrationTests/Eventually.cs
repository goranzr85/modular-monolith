namespace Modular.Notifications.IntegrationTests;

// Test-only polling helper standing in for MassTransit's ITestHarness.Consumed: waits for an async
// consumer's side effect to become visible instead of blocking on an in-memory "message consumed" signal.
internal static class Eventually
{
    public static async Task<T?> WaitForAsync<T>(Func<Task<T?>> query, TimeSpan? timeout = null) where T : class
    {
        DateTime deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));

        while (DateTime.UtcNow < deadline)
        {
            T? result = await query();

            if (result is not null)
            {
                return result;
            }

            await Task.Delay(100);
        }

        return null;
    }
}
