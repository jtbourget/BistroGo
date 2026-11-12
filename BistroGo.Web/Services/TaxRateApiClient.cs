using System.Net.Http.Json;
using BistroGo.Core.Interfaces;

namespace BistroGo.Web.Services
{
    /// <summary>
    /// Typed HTTP client for interacting with the Tax Rates API.
    /// </summary>
    public class TaxRateApiClient : ITaxRateApiClient
    {
        private readonly HttpClient _client;

        public TaxRateApiClient(HttpClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Gets the current active tax rate from the API.
        /// </summary>
        /// <returns>The current tax rate as a decimal (e.g., 0.08 for 8%)</returns>
        public async Task<decimal> GetCurrentRateAsync()
        {
            try
            {
                var response = await _client.GetAsync("api/taxrates/current");

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to fetch current tax rate: {response.StatusCode}");
                }

                var data = await response.Content.ReadFromJsonAsync<TaxRateResponse>();
                return data?.Rate ?? 0.08m;
            }
            catch (Exception ex)
            {
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
                var request = new { rate };
                var response = await _client.PostAsJsonAsync("api/taxrates/update", request);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException("Unauthorized: You do not have permission to update the tax rate.");
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        throw new UnauthorizedAccessException("Forbidden: Only managers can update the tax rate.");
                    }

                    var errorData = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                    throw new HttpRequestException($"Server error: {errorData?.Error ?? $"HTTP {response.StatusCode}"}");
                }

                var result = await response.Content.ReadFromJsonAsync<TaxRateUpdateResponse>();
                return result ?? new TaxRateUpdateResponse { Message = "Tax rate updated successfully", NewRate = rate };
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Error updating tax rate: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Response model for fetching the current tax rate.
    /// </summary>
    public class TaxRateResponse
    {
        public decimal Rate { get; set; }
    }

    /// <summary>
    /// Response model for updating the tax rate.
    /// </summary>
    public class TaxRateUpdateResponse
    {
        public string Message { get; set; }
        public decimal NewRate { get; set; }
    }

    /// <summary>
    /// Error response model.
    /// </summary>
    public class ErrorResponse
    {
        public string Error { get; set; }
    }
}
