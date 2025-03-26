using Microsoft.Extensions.DependencyInjection;
using Polly;
using Quartz;

namespace Modular.Catalog.Infrastructure;
public static class ServiceRegistrations
{

    public static IServiceCollection RegisterCatalogsBackgroundJobs(this IServiceCollection services)
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
            var jobKey = new JobKey("CatalogProcessOutboxMessagesJob");

            configure.AddJob<BackgroundJobs.ProcessOutboxMessagesJob>(jobKey, job =>
            {
                job.WithDescription("Process outbox messages for the catalog module")
                    .Build();
            })
            .AddTrigger(trigger => trigger.ForJob(jobKey)
                                        .WithSimpleSchedule(schedule => schedule.WithIntervalInSeconds(10)
                                        .RepeatForever()));

            //configure.UseMicrosoftDependencyInjectionJobFactory();
        });


        // Register the Quartz.NET service and configure it to block shutdown until jobs are complete.
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
    }
}
