using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Middleware;

public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{

    /* 
    Multiple exception handler will be added 
     */
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        return problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    // other values will be set to default values
                    Title = "Internal Server Error.",
                    // Status = StatusCodes.Status500InternalServerError,
                    Detail = "An error occurred while processing your request." // exception.Message
                }
            }
        );
    }
}
