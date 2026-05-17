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

var authEnabled = builder.Configuration.GetValue<bool>("Authentication:Enabled");
if (authEnabled)
{
    var jwtKey = builder.Configuration["JWT_KEY"] ?? throw new InvalidOperationException("JWT_KEY is not configured.");
    var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? "fiap-oficina-auth";
    var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "fiap-oficina-services";

    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options =>
        {
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey))
            };
        });
    builder.Services.AddAuthorization();
}

builder.Services.AddControllers(options =>
{
    if (authEnabled)
    {
        options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter());
    }
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<BudgetCreatedConsumer>();
    x.AddConsumer<PaymentProcessedConsumer>();
    x.AddConsumer<ExecutionFinishedConsumer>();
    x.AddConsumer<OrderCancelledConsumer>();
    x.AddConsumer<ExecutionStartedConsumer>();

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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OSDbContext>();
    dbContext.Database.EnsureCreated();
    
    // Garante que a tabela Users exista caso o banco já existisse na AWS
    dbContext.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Users"" (
            ""Id"" UUID PRIMARY KEY,
            ""Username"" TEXT NOT NULL,
            ""Name"" TEXT NOT NULL,
            ""Role"" TEXT NOT NULL,
            ""Password"" TEXT NOT NULL
        );
    ");
    
    if (!dbContext.Users.Any())
    {
        dbContext.Users.Add(new FiapOficina.OSService.Api.Models.User
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Name = "System Operator",
            Role = "Operator",
            Password = "admin" // NOSONAR
        });
        dbContext.SaveChanges();
    }
}

app.MapDefaultEndpoints();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}
else
{
    app.UseAuthorization();
}

app.MapControllers();

app.Run();

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public partial class Program { }
