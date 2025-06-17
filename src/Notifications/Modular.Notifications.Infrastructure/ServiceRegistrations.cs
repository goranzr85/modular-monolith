using Microsoft.Extensions.DependencyInjection;
using Modular.Notifications.Infrastructure.BackgroundJobs;
using Modular.Notifications.Infrastructure.NotificationSenders;
using Quartz;

namespace Modular.Notifications.Infrastructure;
public static class ServiceRegistrations
{
    public static IServiceCollection RegisterNotificationsBackgroundJobs(this IServiceCollection services)
    {
        services.AddKeyedScoped<INotificationSender, EmailNotificationsSender>(EmailNotificationsSender.Key);
        services.AddKeyedScoped<INotificationSender, SmsNotificationsSender>(SmsNotificationsSender.Key);
        services.AddScoped<INotificationSender, NotificationSenderFactory>();
        services.RegisterQuartz();

        return services;
    }

    private static void RegisterQuartz(this IServiceCollection services)
    {
        services.AddQuartz(configure =>
        {
            var jobKey = new JobKey("NotificationsProcessOutboxMessagesJob");

            configure.AddJob<ProcessInboxMessagesJob>(jobKey, job =>
            {
                job.WithDescription("Process outbox messages for the catalog module")
                    .Build();
            })
            .AddTrigger(trigger => trigger.ForJob(jobKey)
                                        .WithSimpleSchedule(schedule => schedule.WithIntervalInSeconds(10)
                                        .RepeatForever()));

        });


        // Register the Quartz.NET service and configure it to block shutdown until jobs are complete.
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
    }
}
