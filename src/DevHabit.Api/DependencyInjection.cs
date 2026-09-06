using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Middleware;
using DevHabit.Api.Services;
using DevHabit.Api.Services.Sorting;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json.Serialization;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DevHabit.Api;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddControllers(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers(options =>
            {
                options.ReturnHttpNotAcceptable = true;
            })
            // Replace System.Text.Json with Newtonsoft.Json
            .AddNewtonsoftJson(options =>
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver())
            .AddXmlSerializerFormatters();

        builder.Services.AddOpenApi();
        return builder;
    }

    public static WebApplicationBuilder AddErrorHandling(this WebApplicationBuilder builder)
    {
        // Customize the ProblemDetails response to include the requestId in the extensions
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
            };
        });

        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
        // Add global exception handler
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        return builder;
    }

    public static WebApplicationBuilder AddDatabase(this WebApplicationBuilder builder)
    {
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

        return builder;
    }

    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
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

        return builder;
    }


    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        // Scans assembly for anything implementing `AbstractValidator<CreateTagDto>` and register with DI
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();


        builder.Services.AddTransient<SortMappingProvider>();

        builder.Services.AddSingleton<ISortMappingDefinition, SortMappingDefinition<HabitDto, Habit>>(_ =>
            HabitMappings.SortMapping);

        builder.Services.AddTransient<DataShapingService>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<LinkService>();

        return builder;
    }
}
