using FiapOficina.OSService.Api.Consumers;
using FiapOficina.OSService.Api.Infrastructure;
using FiapOficina.OSService.Api.Models;
using Amazon.SQS;
using Amazon.SimpleNotificationService;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("postgres") ?? "Host=localhost;Database=osdb;Username=postgres;Password=postgres";
builder.Services.AddDbContext<OSDbContext>(options => 
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<BudgetCreatedConsumer>();
    x.AddConsumer<PaymentProcessedConsumer>();
    x.AddConsumer<ExecutionFinishedConsumer>();
    x.AddConsumer<OrderCancelledConsumer>();

    x.UsingAmazonSqs((context, cfg) =>
    {
        var sqsUrl = builder.Configuration["AWS:Service:SQS:ServiceURL"];
        cfg.Host("us-east-1", h => 
        { 
            if (!string.IsNullOrEmpty(sqsUrl))
            {
                h.Config(new AmazonSQSConfig { ServiceURL = sqsUrl });
                h.Config(new AmazonSimpleNotificationServiceConfig { ServiceURL = sqsUrl });
            }
        });
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
