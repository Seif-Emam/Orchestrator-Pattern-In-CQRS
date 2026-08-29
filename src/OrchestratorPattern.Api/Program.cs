using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OrchestratorPattern.Api.Common.Behaviors;
using OrchestratorPattern.Api.Common.Middleware;
using OrchestratorPattern.Api.Common.Persistence;
using OrchestratorPattern.Api.Common.Persistence.Seed;
using OrchestratorPattern.Api.Features.Inventory;
using OrchestratorPattern.Api.Features.Orders;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration;
using OrchestratorPattern.Api.Features.Orders.Commands.Checkout.Orchestration.Steps;
using OrchestratorPattern.Api.Features.Payments;
using OrchestratorPattern.Api.Features.Shipping;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// 1. Dependency Injection & Service Registration
// -----------------------------------------------------------------------------

// Database Context (SQL Server)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    });
});

// MediatR & Pipeline Behaviors
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Checkout Orchestrator & Workflow Step Services
builder.Services.AddScoped<ICheckoutOrchestrator, CheckoutOrchestrator>();
builder.Services.AddScoped<IOrderValidationStep, OrderValidationStep>();
builder.Services.AddScoped<IInventoryReservationStep, InventoryReservationStep>();
builder.Services.AddScoped<IPaymentProcessingStep, PaymentProcessingStep>();
builder.Services.AddScoped<IShipmentCreationStep, ShipmentCreationStep>();
builder.Services.AddScoped<IFinalizeCheckoutStep, FinalizeCheckoutStep>();

// Centralized Error Handling & ProblemDetails (RFC 7807)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// API Explorer & Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "E-Commerce Checkout API (With Orchestrator Pattern)",
        Version = "v1",
        Description = "Production-quality CQRS with Vertical Slice Architecture demonstrating the Orchestrator Pattern for multi-step Checkout workflows with explicit compensation."
    });
});

var app = builder.Build();

// -----------------------------------------------------------------------------
// 2. HTTP Request Pipeline Configuration
// -----------------------------------------------------------------------------

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Checkout API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger at application root
    });
}

app.UseHttpsRedirection();

// -----------------------------------------------------------------------------
// 3. Feature Endpoint Registrations (Vertical Slice Architecture)
// -----------------------------------------------------------------------------

app.MapOrderEndpoints();
app.MapInventoryEndpoints();
app.MapPaymentEndpoints();
app.MapShippingEndpoints();

// -----------------------------------------------------------------------------
// 4. Automatic Database Migration & Seeding
// -----------------------------------------------------------------------------

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        if (dbContext.Database.IsRelational())
        {
            logger.LogInformation("Applying database migrations...");
            await dbContext.Database.MigrateAsync();
        }
        logger.LogInformation("Seeding database with initial catalog and customers...");
        await DatabaseSeeder.SeedAsync(dbContext);
        logger.LogInformation("Database ready.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database migration/seeding.");
    }
}

await app.RunAsync();

public partial class Program { }
