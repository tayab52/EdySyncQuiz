using Application.DataTransferModels.ResponseModel;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace PresentationAPI.Middlewares
{
    public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, GetOptions());
            }
        }

        private static JsonSerializerOptions GetOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex, JsonSerializerOptions options)
        {
            ResponseVM response = ResponseVM.Instance;
            var statusCode = (int)HttpStatusCode.InternalServerError;
            var errorMessage = "An unexpected error occurred.";

            logger.LogError(ex, "Exception occurred while processing request {Method} {Path}", context.Request.Method, context.Request.Path);

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
                logger.LogWarning("The response has already started, the error handling middleware will not modify the response.");
                return;
            }

            response.StatusCode = statusCode;
            response.ErrorMessage = errorMessage;

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}
