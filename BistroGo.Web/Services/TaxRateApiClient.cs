using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Web.Services
{
    /// <summary>
    /// Typed HTTP client for interacting with the Tax Rates API.
    /// </summary>
    public class TaxRateApiClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<TaxRateApiClient> _logger;

        public TaxRateApiClient(HttpClient client, ILogger<TaxRateApiClient>? logger = null)
        {
            _client = client;
            _logger = logger ?? NullLogger<TaxRateApiClient>.Instance;
        }

        /// <summary>
        /// Gets the current active tax rate from the API.
        /// </summary>
        /// <returns>The current tax rate as a decimal (e.g., 0.08 for 8%)</returns>
        public async Task<decimal> GetCurrentRateAsync()
        {
            try
            {
                if (_client.BaseAddress == null)
                {
                    _logger.LogWarning("BaseAddress is not configured for TaxRateApiClient; using default rate 0.08");
                    return 0.08m;
                }

                _logger.LogDebug("Calling GET api/taxrates/current");
                var response = await _client.GetAsync("api/taxrates/current");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch tax rate from API. Status: {StatusCode}", response.StatusCode);
                    throw new HttpRequestException($"Failed to fetch current tax rate: {response.StatusCode}");
                }

                var data = await response.Content.ReadFromJsonAsync<TaxRateResponse>();
                var rate = data?.Rate ?? 0.08m;
                _logger.LogInformation("Fetched current tax rate {Rate} from API", rate);
                return rate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching current tax rate from API");
                throw new HttpRequestException($"Error fetching current tax rate: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Updates the tax rate (manager only).
        /// </summary>
        /// <param name="rate">The new tax rate as a decimal (0.00 to 0.20)</param>
        /// <returns>The server response</returns>
        public async Task<TaxRateUpdateResponse> UpdateRateAsync(decimal rate)
        {
            try
            {
                _logger.LogInformation("Calling POST api/taxrates/update to set rate to {Rate}", rate);
                var request = new { rate };
                var response = await _client.PostAsJsonAsync("api/taxrates/update", request);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _logger.LogWarning("Unauthorized attempt to update tax rate to {Rate}", rate);
                        throw new UnauthorizedAccessException("Unauthorized: You do not have permission to update the tax rate.");
                    }

                    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                    var errorMsg = error?.Error ?? $"Failed with status {response.StatusCode}";
                    _logger.LogWarning("Failed to update tax rate: {ErrorMessage}", errorMsg);
                    throw new InvalidOperationException($"Tax rate update failed: {errorMsg}");
                }

                var result = await response.Content.ReadFromJsonAsync<TaxRateUpdateResponse>();
                _logger.LogInformation("Successfully updated tax rate to {Rate}", rate);
                return result ?? new TaxRateUpdateResponse { Message = "Tax rate updated successfully", NewRate = rate };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating tax rate to {Rate}", rate);
                throw;
            }
        }
    }

    /// <summary>
    /// Response model for GET /api/taxrates/current
    /// </summary>
    public class TaxRateResponse
    {
        public decimal Rate { get; set; }
    }

    /// <summary>
    /// Response model for POST /api/taxrates/update
    /// </summary>
    public class TaxRateUpdateResponse
    {
        public string Message { get; set; } = string.Empty;
        public decimal NewRate { get; set; }
    }

    /// <summary>
    /// Error response model
    /// </summary>
    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
    }
}
