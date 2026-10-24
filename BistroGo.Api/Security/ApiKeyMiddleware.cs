using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;

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

        /// <summary>
        /// Runs once at startup. Reads ApiSettings:Key from configuration and stores it as bytes
        /// for comparison. Throws if the key is missing, so the API refuses to start unprotected.
        /// </summary>
        public ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
        {
            _next = next;

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
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized"
                });
                return;
            }

            await _next(context);
        }
    }
}