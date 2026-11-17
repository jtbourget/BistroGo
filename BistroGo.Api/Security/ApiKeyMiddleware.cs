using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Api.Security
{
    /// <summary>
    /// Rejects any /api request that doesn't carry the shared secret in X-Api-Key.
    /// Only the Web app knows the key, so only the Web app can call the API.
    /// </summary>
    public class ApiKeyMiddleware
    {
        public const string HeaderName = "X-Api-Key";

        private readonly RequestDelegate _next;
        private readonly byte[] _expectedKey;
        private readonly ILogger<ApiKeyMiddleware> _logger;

        /// <summary>
        /// Runs once at startup. Reads ApiSettings:Key from configuration and stores it as bytes
        /// for comparison. Throws if the key is missing, so the API refuses to start unprotected.
        /// </summary>
        public ApiKeyMiddleware(RequestDelegate next, IConfiguration config, ILogger<ApiKeyMiddleware>? logger = null)
        {
            _next = next;
            _logger = logger ?? NullLogger<ApiKeyMiddleware>.Instance;

            var key = config["ApiSettings:Key"];
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("ApiSettings:Key is not configured.");

            _expectedKey = Encoding.UTF8.GetBytes(key);
        }

        /// <summary>
        /// Runs for every incoming request. Non-/api paths pass straight through. For /api paths it
        /// compares the X-Api-Key header with the configured key; a missing or wrong key gets a 401
        /// ProblemDetails response and the request stops here, before any controller runs.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            var provided = Encoding.UTF8.GetBytes(context.Request.Headers[HeaderName].ToString());

            // Constant-time comparison, so response timing can't reveal how close a guess was.
            if (!CryptographicOperations.FixedTimeEquals(provided, _expectedKey))
            {
                _logger.LogWarning("Unauthorized access attempt to {Path} from IP {RemoteIp}",
                    context.Request.Path, context.Connection.RemoteIpAddress);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized"
                });
                return;
            }

            _logger.LogDebug("API key verified successfully for {Path}", context.Request.Path);
            await _next(context);
        }
    }
}
