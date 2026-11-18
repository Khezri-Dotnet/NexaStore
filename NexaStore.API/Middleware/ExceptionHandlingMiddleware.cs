
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace NexaStore.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
            when (context.RequestAborted.IsCancellationRequested)
        {
            Log.Information(
                "Request was cancelled. CorrelationId: {CorrelationId}",
                context.TraceIdentifier);
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Unhandled exception occurred. CorrelationId: {CorrelationId}",
                context.TraceIdentifier);

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode =
                StatusCodes.Status500InternalServerError;

            context.Response.Headers["X-Correlation-ID"] =
                context.TraceIdentifier;

            await context.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred.",
                    Extensions =
                    {
                        ["correlationId"] = context.TraceIdentifier
                    }
                });
        }
    }
}
