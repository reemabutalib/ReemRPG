using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ReemRPG.Middleware
{
    /// <summary>
    /// Middleware to globally handle exceptions in the application.
    /// Catches unhandled exceptions, logs them, and returns a JSON response with error details.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        /// <summary>
        /// Constructor for GlobalExceptionMiddleware.
        /// </summary>
        /// <param name="next">The next middleware component in the request pipeline.</param>
        /// <param name="logger">Logger for capturing error details.</param>
        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Middleware execution method that wraps request processing in a try-catch block.
        /// </summary>
        /// <param name="context">HttpContext of the current request.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Proceed to the next middleware in the pipeline.
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log the error details for debugging and monitoring.
                _logger.LogError(ex, "An unhandled exception occurred.");

                // Handle the exception and return a standardized error response.
                await HandleExceptionAsync(context, ex);
            }
        }

        /// <summary>
        /// Handles exceptions by returning a standardized JSON error response.
        /// </summary>
        /// <param name="context">HttpContext of the request where the exception occurred.</param>
        /// <param name="exception">The caught exception.</param>
        /// <returns>A JSON response with error details.</returns>
        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Define the error response structure
            var response = new
            {
                StatusCode = (int)HttpStatusCode.InternalServerError, // HTTP 500 error
                Message = "An unexpected error occurred.", // Generic error message for users
                Details = exception.Message // Include exception message for debugging (avoid exposing sensitive info)
            };

            // Set response content type to JSON
            context.Response.ContentType = "application/json";

            // Set HTTP status code to 500 (Internal Server Error)
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // Serialize the error response and write it to the response body
            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
