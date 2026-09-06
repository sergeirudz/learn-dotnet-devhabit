using DevHabit.Api;
using DevHabit.Api.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder
    .AddApiServices()
    .AddErrorHandling()
    .AddDatabase()
    .AddObservability()
    .AddApplicationServices();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Run migrations when I start the app in development environment
    await app.ApplyMigrationsAsync(); // src/DevHabit.Api/Extensions/DatabaseExtensions.cs
}

app.UseHttpsRedirection();
app.UseExceptionHandler();
app.MapControllers();

await app.RunAsync().ConfigureAwait(false);
