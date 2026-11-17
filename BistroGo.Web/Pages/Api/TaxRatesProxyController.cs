using BistroGo.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Web.Pages.Api
{
    /// <summary>
    /// Local API endpoints for tax rate management in the Razor Pages app.
    /// These endpoints proxy calls to the BistroGo.Api service with proper authentication.
    /// </summary>
    [ApiController]
    [Route("api/taxrates")]
    public class TaxRatesProxyController : ControllerBase
    {
        private readonly TaxRateApiClient? _taxRateApiClient;
        private readonly ILogger<TaxRatesProxyController> _logger;

        public TaxRatesProxyController(ILogger<TaxRatesProxyController>? logger = null, TaxRateApiClient? taxRateApiClient = null)
        {
            _taxRateApiClient = taxRateApiClient;
            _logger = logger ?? NullLogger<TaxRatesProxyController>.Instance;
        }

        /// <summary>
        /// Gets the current active tax rate.
        /// </summary>
        /// <returns>The current tax rate as a decimal</returns>
        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentRate()
        {
            try
            {
                if (_taxRateApiClient == null)
                {
                    _logger.LogWarning("TaxRateApiClient is not registered; returning fallback rate 0.08");
                    return Ok(new { rate = 0.08m });
                }

                _logger.LogDebug("Proxying GET current tax rate to API");
                var rate = await _taxRateApiClient.GetCurrentRateAsync();
                return Ok(new { rate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching current tax rate from API; returning fallback 0.08");
                return Ok(new { rate = 0.08m });
            }
        }

        /// <summary>
        /// Updates the tax rate (manager only).
        /// </summary>
        /// <param name="updateRequest">Object containing the new rate</param>
        /// <returns>Success/failure response</returns>
        [HttpPost("update")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateRate([FromBody] UpdateTaxRateRequest updateRequest)
        {
            try
            {
                if (_taxRateApiClient == null)
                {
                    _logger.LogWarning("TaxRateApiClient is not registered; cannot update tax rate");
                    return StatusCode(StatusCodes.Status503ServiceUnavailable,
                        new { error = "Tax rate service is unavailable" });
                }

                if (updateRequest == null || updateRequest.Rate < 0)
                {
                    _logger.LogWarning("Invalid tax rate value in update request");
                    return BadRequest(new { error = "Invalid rate value" });
                }

                _logger.LogInformation("Proxying POST tax rate update to API: {NewRate}", updateRequest.Rate);
                var result = await _taxRateApiClient.UpdateRateAsync(updateRequest.Rate);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized tax rate update attempt by {UserName}", User.Identity?.Name);
                return Forbid();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error updating tax rate via API");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { error = "Failed to update tax rate" });
            }
        }
    }

    /// <summary>
    /// Request model for updating the tax rate.
    /// </summary>
    public class UpdateTaxRateRequest
    {
        /// <summary>The new tax rate as a decimal (0.00 to 0.20)</summary>
        public decimal Rate { get; set; }
    }
}
