using Carter;
using Modular.Catalog;
using Modular.Catalog.Infrastructure;
using Modular.Customers;
using Modular.Warehouse;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.RegisterCustomerModule(builder.Configuration);
builder.Services
    .RegisterCatalogModule(builder.Configuration)
    .RegisterCatalogsBackgroundJobs();
builder.Services.AddWarehouse(builder.Configuration);
builder.Services.AddCarter();

//builder.Services.AddQuartz(configure =>
//{
//    var jobKey = new JobKey("CatalogProcessOutboxMessagesJob");

//    configure.AddJob<ProcessOutboxMessagesJob>(jobKey, job =>
//    {
//        job.WithDescription("Process outbox messages for the catalog module")
//            .Build();
//    })
//    .AddTrigger(trigger => trigger.ForJob(jobKey)
//                                .WithSimpleSchedule(schedule => schedule.WithIntervalInSeconds(10)
//                                .RepeatForever()));
//});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapCarter();

await app.RunAsync();
