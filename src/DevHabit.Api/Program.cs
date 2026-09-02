using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
    .WithTracing(tracing =>
        tracing.AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation()
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
}

app.UseHttpsRedirection();


app.MapControllers();

await app.RunAsync().ConfigureAwait(false);
