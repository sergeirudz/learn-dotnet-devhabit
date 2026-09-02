using DevHabit.Api.Database;
using DevHabit.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(
            // src/DevHabit.Api/appsettings.Development.json
            builder.Configuration.GetConnectionString("Database"), 
            npsqlOptions =>
                npsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName,
            // assign name and schema for migration history table
                    Schemas.Application) 

        )
        .UseSnakeCaseNamingConvention();
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
    .WithTracing(tracing =>
        tracing.AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddNpgsql()
    )
    // Setup opentelemetry and export metrics, using Opentelemetry exporter
    .WithMetrics(metrics => metrics
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation())
    // Setup a global exporter for opentelemetry data
    .UseOtlpExporter();

// Add logging
builder.Logging.AddOpenTelemetry(options =>
{
    // Allow to include things like transaction IDs, user IDs, correlation IDs, etc.
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Run migrations when I start the app in development environment
    await app.ApplyMigrationsAsync(); // src/DevHabit.Api/Extensions/DatabaseExtensions.cs
}

app.UseHttpsRedirection();


app.MapControllers();

await app.RunAsync().ConfigureAwait(false);
