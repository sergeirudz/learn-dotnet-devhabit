using Asp.Versioning;
using DevHabit.Api.Database;
using DevHabit.Api.DTOs.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Middleware;
using DevHabit.Api.Services;
using DevHabit.Api.Services.Sorting;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
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
    public static WebApplicationBuilder AddApiServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllers(options =>
            {
                // Accept application/json
                options.ReturnHttpNotAcceptable = true; // return 406 when unsupported format is requested
            })
            // add support for JSON Patch. Will replace default ASP.NET systemtext.json serializer
            .AddNewtonsoftJson(options =>
                options.SerializerSettings.ContractResolver =
                    new CamelCasePropertyNamesContractResolver()) // this will make sure JSON properties are camelCase
            .AddXmlSerializerFormatters(); // add XML formatter support. Header "Accept: application/xml"

        // Accept application/vnd.dev-habit.hateoas+json
        builder.Services.Configure<MvcOptions>(options =>
        {
            NewtonsoftJsonOutputFormatter formatter = options.OutputFormatters
                .OfType<NewtonsoftJsonOutputFormatter>()
                .First();

            // This allows to set multiple supported API versions
            formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.JsonV1);
            formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.JsonV2);
            formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.HateoasJson);
            formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.HateoasJsonV1);
            formatter.SupportedMediaTypes.Add(CustomMediaTypeNames.Application.HateoasJsonV2);
        });

        // API Versioning
        builder.Services.AddApiVersioning(options =>

            {
                options.DefaultApiVersion = new ApiVersion(1.0);
                options.AssumeDefaultVersionWhenUnspecified = true; // use default version if not specified
                options.ReportApiVersions = true; // return supported versions in response headers
                // options.ApiVersionSelector = new CurrentImplementationApiVersionSelector(options); // detect which API version to use, the latest API version implemented will be used
                options.ApiVersionSelector =
                    new DefaultApiVersionSelector(options); // use default version if not specified

                // [Route("v{version:apiVersion}/habits")]
                // options.ApiVersionReader = new UrlSegmentApiVersionReader(); // read version from URL segment  /v1/
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new MediaTypeApiVersionReader(),
                    new MediaTypeApiVersionReaderBuilder()
                        .Template("application/vnd.dev-habit.hateoas.{version}+json")
                        .Build()

                    // new UrlSegmentApiVersionReader(), // read version from URL segment
                    // new HeaderApiVersionReader("x-api-version") // read version from custom header
                );
            })
            .AddMvc();

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
