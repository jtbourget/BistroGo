using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Api.Errors
{
    /// <summary>
    /// Catches any exception a controller or repository didn't handle, logs the full
    /// details on the server, and returns a short ProblemDetails response to the caller.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Maps the exception to an HTTP status and writes the response.
        /// Returns true to tell ASP.NET the exception has been dealt with.
        /// </summary>
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // Full detail (including the stack trace) goes to the server log only.
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

            var (status, title) = exception switch
            {
                DbUpdateException => (StatusCodes.Status409Conflict, "The request conflicts with the current data."),
                ArgumentException => (StatusCodes.Status400BadRequest, "The request was not valid."),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            // Caller only ever gets the title, no internal details.
            var problem = new ProblemDetails { Status = status, Title = title };
            problem.Extensions["traceId"] = httpContext.TraceIdentifier;

            httpContext.Response.StatusCode = status;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }
    }
}