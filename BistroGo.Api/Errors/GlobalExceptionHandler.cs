using BistroGo.Core.Logging;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BistroGo.Api.Errors
{
    /// <summary>
    /// Catches any exception a controller or repository didn't handle, logs the full
    /// details on the server with correlation ID context, and returns a short ProblemDetails response to the caller.
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
            var correlationId = httpContext.Items[CorrelationConstants.PropertyName]?.ToString()
                ?? httpContext.Request.Headers[CorrelationConstants.HeaderName].FirstOrDefault()
                ?? httpContext.TraceIdentifier;

            // Full detail (including the stack trace) goes to the server log with correlation ID.
            _logger.LogError(exception, "Unhandled exception for {Method} {Path} [CorrelationId: {CorrelationId}]",
                httpContext.Request.Method, httpContext.Request.Path, correlationId);

            var (status, title) = exception switch
            {
                DbUpdateException => (StatusCodes.Status409Conflict, "The request conflicts with the current data."),
                ArgumentException => (StatusCodes.Status400BadRequest, "The request was not valid."),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            // Caller gets the title, traceId, and correlationId, but no internal sensitive details.
            var problem = new ProblemDetails { Status = status, Title = title };
            problem.Extensions["traceId"] = httpContext.TraceIdentifier;
            problem.Extensions["correlationId"] = correlationId;

            httpContext.Response.StatusCode = status;
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }
    }
}
