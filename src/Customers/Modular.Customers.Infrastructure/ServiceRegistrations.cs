using Microsoft.Extensions.DependencyInjection;
using Polly;
using Quartz;

namespace Modular.Customers.Infrastructure;
public static class ServiceRegistrations
{
    public static IServiceCollection RegisterCustomersBackgroundJobs(this IServiceCollection services)
    {
        services.RegisterQuartz();

        services.AddResiliencePipeline(Constants.ResiliencePipelineName, builder =>
        {
            builder.AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                Delay = TimeSpan.FromSeconds(2),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            });
        });

        return services;
    }

    private static void RegisterQuartz(this IServiceCollection services)
    {
        services.AddQuartz(configure =>
        {
            var jobKey = new JobKey("CustomersProcessOutboxMessagesJob");

            configure.AddJob<BackgroundJobs.ProcessOutboxMessagesJob>(jobKey, job =>
            {
                job.WithDescription("Process outbox messages for the customers module")
                    .Build();
            })
            .AddTrigger(trigger => trigger.ForJob(jobKey)
                                        .WithSimpleSchedule(schedule => schedule.WithIntervalInSeconds(10)
                                        .RepeatForever()));
        });

        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
    }
}
