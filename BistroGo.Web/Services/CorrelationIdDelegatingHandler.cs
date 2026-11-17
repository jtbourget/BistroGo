using System.Diagnostics;
using BistroGo.Core.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Web.Services
{
    /// <summary>
    /// Delegating HTTP handler that propagates the Correlation ID from the ambient HttpContext
    /// to outgoing HTTP requests destined for the backend API, enabling end-to-end request tracing.
    /// </summary>
    public class CorrelationIdDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CorrelationIdDelegatingHandler> _logger;

        public CorrelationIdDelegatingHandler(
            IHttpContextAccessor httpContextAccessor,
            ILogger<CorrelationIdDelegatingHandler>? logger = null)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger ?? NullLogger<CorrelationIdDelegatingHandler>.Instance;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            string? correlationId = null;

            if (httpContext != null)
            {
                if (httpContext.Items.TryGetValue(CorrelationConstants.PropertyName, out var idObj) && idObj is string id)
                {
                    correlationId = id;
                }
                else
                {
                    correlationId = httpContext.Request.Headers[CorrelationConstants.HeaderName].FirstOrDefault();
                }
            }

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                if (!request.Headers.Contains(CorrelationConstants.HeaderName))
                {
                    request.Headers.Add(CorrelationConstants.HeaderName, correlationId);
                }
            }

            _logger.LogInformation("Outbound API Request: {Method} {RequestUri} [CorrelationId: {CorrelationId}]",
                request.Method, request.RequestUri, correlationId ?? "None");

            var sw = Stopwatch.StartNew();
            try
            {
                var response = await base.SendAsync(request, cancellationToken);
                sw.Stop();

                _logger.LogInformation("Outbound API Response: {Method} {RequestUri} returned {StatusCode} in {ElapsedMs}ms",
                    request.Method, request.RequestUri, (int)response.StatusCode, sw.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Outbound API Request Failed: {Method} {RequestUri} after {ElapsedMs}ms",
                    request.Method, request.RequestUri, sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
