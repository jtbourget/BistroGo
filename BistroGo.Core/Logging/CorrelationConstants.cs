namespace BistroGo.Core.Logging
{
    /// <summary>
    /// Shared constants for correlation ID propagation and request tracing.
    /// </summary>
    public static class CorrelationConstants
    {
        /// <summary>
        /// HTTP header used to pass correlation IDs between clients, Web frontend, and API backend.
        /// </summary>
        public const string HeaderName = "X-Correlation-ID";

        /// <summary>
        /// Key name used in log scopes and HttpContext.Items dictionary.
        /// </summary>
        public const string PropertyName = "CorrelationId";
    }
}
