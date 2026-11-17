using System.Diagnostics;
using BistroGo.Core.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BistroGo.Web.Middleware
{
    /// <summary>
    /// Middleware that establishes or extracts an X-Correlation-ID header, attaches it to the
    /// response headers, and starts an ILogger logging scope with the CorrelationId property
    /// for tracing frontend request lifecycles.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers[CorrelationConstants.HeaderName].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString("D");
            }

            context.Items[CorrelationConstants.PropertyName] = correlationId;

            // Ensure correlation ID is on response headers for client visibility
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(CorrelationConstants.HeaderName))
                {
                    context.Response.Headers[CorrelationConstants.HeaderName] = correlationId;
                }
                return Task.CompletedTask;
            });

            var scopeState = new Dictionary<string, object>
            {
                [CorrelationConstants.PropertyName] = correlationId,
                ["RequestPath"] = context.Request.Path.Value ?? string.Empty,
                ["RequestMethod"] = context.Request.Method
            };

            using (_logger.BeginScope(scopeState))
            {
                var stopwatch = Stopwatch.StartNew();
                _logger.LogInformation("HTTP Web Request Started: {Method} {Path} [CorrelationId: {CorrelationId}]",
                    context.Request.Method, context.Request.Path, correlationId);

                try
                {
                    await _next(context);

                    stopwatch.Stop();
                    _logger.LogInformation("HTTP Web Request Finished: {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                        context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogError(ex, "HTTP Web Request Failed: {Method} {Path} after {ElapsedMs}ms",
                        context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);
                    throw;
                }
            }
        }
    }
}
