using Carter;
using Marten;
using MassTransit;
using Modular.Catalog;
using Modular.Catalog.Infrastructure;
using Modular.Customers;
using Modular.Orders;
using Modular.Warehouse;
using Modular.WebApi.MIddlewares;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.RegisterCustomerModule(builder.Configuration);
builder.Services
    .RegisterCatalogModule(builder.Configuration)
    .RegisterCatalogsBackgroundJobs();
builder.Services.RegisterOrderModule(builder.Configuration);
builder.Services.AddWarehouse(builder.Configuration);
builder.Services.AddCarter();
builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);



builder.Services.AddMassTransit(mt =>
{
    mt.SetKebabCaseEndpointNameFormatter();
    mt.AddOrderConsumers();
    mt.AddWarehouseConsumers();

    mt.AddConfigureEndpointsCallback((context, name, cfg) =>
    {
        cfg.UseMessageRetry(r => r.Immediate(5));
    });

    mt.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(new Uri("rabbitmq://localhost:5673"), h =>
        {
            h.Username("admin");
            h.Password("admin");
        });

        //cfg.ConfigureEndpoints(context);

        cfg.ReceiveOrderEndpoints(context);
        cfg.WarehouseEndpoints(context);
    });


    //mt.AddConsumers(typeof(Modular.Orders.ServiceRegistration).Assembly);  // ProjectA

});

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

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.MapCarter();

await app.RunAsync();
