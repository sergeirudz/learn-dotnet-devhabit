using DevHabit.Api;
using DevHabit.Api.Extensions;
using DevHabit.Api.Middleware;
using DevHabit.Api.Settings;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder
    .AddApiServices()
    .AddErrorHandling()
    .AddDatabase()
    .AddObservability()
    .AddApplicationServices()
    .AddAuthenticationServices()
    .AddBackgroundJobs()
    .AddCorsPolicy()
    .AddRateLimiting();

WebApplication app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DevHabit API v1");
});

app.MapScalarApiReference(options =>
{
    options.WithOpenApiRoutePattern("/swagger/v1/swagger.json");
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Run migrations when I start the app in development environment
    await app.ApplyMigrationsAsync(); // src/DevHabit.Api/Extensions/DatabaseExtensions.cs

    await app.SeedInitialDataAsync();
}

app.UseHttpsRedirection();
app.UseExceptionHandler();

app.UseCors(CorsOptions.PolicyName);
app.UseResponseCaching();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.UseMiddleware<ETagMiddleware>();

app.MapControllers();

await app.RunAsync().ConfigureAwait(false);
// For integration tests
public partial class Program;
