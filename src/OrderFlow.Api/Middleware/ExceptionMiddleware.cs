using System.Text.Json;
using FluentValidation;
using OrderFlow.Domain.Exceptions;

namespace OrderFlow.Api.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errors) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status400BadRequest,
                ve.Errors.Select(e => e.ErrorMessage).ToArray()),

            DomainException de => (
                StatusCodes.Status422UnprocessableEntity,
                new[] { de.Message }),

            OrderNotFoundException nfe => (
                StatusCodes.Status404NotFound,
                new[] { nfe.Message }),

            _ => (StatusCodes.Status500InternalServerError, new[] { "An unexpected error occurred." })
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new { errors };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
