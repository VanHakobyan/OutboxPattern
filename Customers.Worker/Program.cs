using Customers.Worker;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<AppDbContext>(o =>
    {
        o.DuplicateDetectionWindow = TimeSpan.FromSeconds(30);
        o.UsePostgres();
    });
    
    x.SetKebabCaseEndpointNameFormatter();

    var assembly = typeof(Program).Assembly;
    
    x.AddConsumers(assembly);
    x.AddActivities(assembly);

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);

        cfg.Host("localhost", 5672, "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });

    //x.UsingAmazonSqs((ctx, cfg) =>
    //{
    //    cfg.Host("eu-west-2", _ => {});
    //    cfg.ConfigureEndpoints(ctx);
    //});
});

builder.Services.AddDbContext<AppDbContext>(x =>
{
    x.UseNpgsql(builder.Configuration["Database:ConnectionString"]!, opt =>
    {
        opt.EnableRetryOnFailure(5);
    });
});

var app = builder.Build();

app.Run();
