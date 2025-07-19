using Application.DataTransferModels.ResponseModel;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace PresentationAPI.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
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

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            ResponseVM response = ResponseVM.Instance;
            var statusCode = (int)HttpStatusCode.InternalServerError;
            var errorMessage = "An unexpected error occurred.";

            _logger.LogError(ex, "Exception occurred while processing request {Method} {Path}", context.Request.Method, context.Request.Path);

            switch (ex)
            {
                case DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("Cannot insert duplicate key") == true:
                    statusCode = (int)HttpStatusCode.Conflict;
                    errorMessage = "A user with this email already exists.";
                    break;

                case UnauthorizedAccessException:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    errorMessage = "You are not authorized to access this resource.";
                    break;

                case ArgumentNullException or ArgumentException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    errorMessage = "Invalid input provided.";
                    break;

                case NotImplementedException:
                    statusCode = (int)HttpStatusCode.NotImplemented;
                    errorMessage = "This feature is not implemented.";
                    break;
            }

            if (context.Response.HasStarted)
            {
                _logger.LogWarning("The response has already started, the error handling middleware will not modify the response.");
                return;
            }

            response.StatusCode = statusCode;
            response.ErrorMessage = errorMessage;

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}
