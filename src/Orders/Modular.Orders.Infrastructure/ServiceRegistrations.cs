using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Modular.Orders.Infrastructure.BackgroundJobs;
using Quartz;

namespace Modular.Orders.Infrastructure;
public static class ServiceRegistrations
{
    public static IServiceCollection RegisterCatalogsBackgroundJobs(this IServiceCollection services)
    {
        services.RegisterQuartz();

        return services;
    }

    private static void RegisterQuartz(this IServiceCollection services)
    {
        services.AddQuartz(configure =>
        {
            var jobKey = new JobKey("CatalogProcessOutboxMessagesJob");

            configure.AddJob<ProcessOutboxMessagesJob>(jobKey, job =>
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
