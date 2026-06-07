using System.Text.Json;
using System.Text.Json.Serialization;
using UNI_EDU_Backend.Application.Exceptions;
using ApplicationException = UNI_EDU_Backend.Application.Exceptions.ApplicationException;
using UnauthorizedAccessException = UNI_EDU_Backend.Application.Exceptions.UnauthorizedAccessException;

namespace UNI_EDU_Backend.API.Middleware;

public class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    private static readonly JsonSerializerOptions s_writeOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger = logger;

    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext httpContext, Exception ex)
    {
        var statusCode = GetStatusCode(ex);

        var response = new ErrorResponse
        {
            StatusCode = statusCode,
            Message = ex.Message,
            Title = GetTitle(ex),
            Errors = GetErrors(ex)
        };

        WriteLog(ex, response.Errors);

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = statusCode;

        var jsonResponse = JsonSerializer.Serialize(response, s_writeOptions);

        await httpContext.Response.WriteAsync(jsonResponse);
    }

    private void WriteLog(Exception ex, List<ErrorDetail>? errorDetails)
    {
        switch (ex)
        {
            case ValidationException validationException:
                _logger.LogWarning(validationException, "Validation error: {ValidationErrors}", string.Join(", ", errorDetails!.Select(e => $"{e.Field}: {e.Messages}")));
                break;

            case NotFoundException notFoundException:
                _logger.LogWarning(notFoundException, "Resource not found.");
                break;

            case UnauthorizedAccessException unauthorizedAccessException:
                _logger.LogWarning(unauthorizedAccessException, "Unauthorized access attempt.");
                break;

            case ForbiddenAccessException forbiddenAccessException:
                _logger.LogWarning(forbiddenAccessException, "Forbidden access attempt.");
                break;

            case BadRequestException badRequestException:
                _logger.LogWarning(badRequestException, "Having bad request.");
                break;

            default:
                _logger.LogError(ex, "Unhandled exception: {ExcetionType} - {Message}", ex.GetType().Name, ex.Message);
                break;
        }
    }

    private List<ErrorDetail>? GetErrors(Exception ex)
    {
        List<ErrorDetail>? errors = null;

        if (ex is ValidationException validationException)
            errors = validationException.ErrorsDictionary.Select(e => new ErrorDetail
            {
                Field = e.Key,
                Messages = e.Value
            }).ToList();

        return errors;
    }

    private string GetTitle(Exception ex)
    {
        return ex switch
        {
            ApplicationException applicationException => applicationException.Title,
            _ => "Server Error"
        };
    }

    private int GetStatusCode(Exception ex)
    {
        return ex switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            BadRequestException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            ForbiddenAccessException => StatusCodes.Status403Forbidden,
            ValidationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}

public class ErrorResponse
{
    public int StatusCode { get; set; }

    public string? Message { get; set; }

    public string? Title { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<ErrorDetail>? Errors { get; set; }
}

public class ErrorDetail
{
    public string Field { get; set; } = default!;

    public string[] Messages { get; set; } = default!;
}
