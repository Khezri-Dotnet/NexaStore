
using Microsoft.AspNetCore.Mvc;
using Serilog;
using NexaStore.Application.Common.Exceptions;

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

            var (statusCode, title, detail) = ex switch
            {
                UnauthorizedAccessException =>
                    (StatusCodes.Status401Unauthorized,
                     "Unauthorized",
                     "Invalid email or password."),                     

                     RegistrationValidationException =>
                (StatusCodes.Status400BadRequest,
                "Bad Request",
                "User registration failed."),

                ArgumentException =>
                    (StatusCodes.Status400BadRequest,
                     "Bad Request",
                     "The request contains invalid data."),

                _ =>
                    (StatusCodes.Status500InternalServerError,
                     "Internal Server Error",
                     "An unexpected error occurred."),
                     
            };

            context.Response.Clear();
            context.Response.StatusCode = statusCode;

            context.Response.Headers["X-Correlation-ID"] =
                context.TraceIdentifier;

            await context.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = detail,
                    Extensions =
                    {
                        ["correlationId"] = context.TraceIdentifier
                    }
                });
        }
    }
}
