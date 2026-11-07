using BistroGo.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        private readonly TaxRateApiClient _taxRateApiClient;
        private readonly ILogger<TaxRatesProxyController> _logger;

        public TaxRatesProxyController(TaxRateApiClient taxRateApiClient, ILogger<TaxRatesProxyController> logger)
        {
            _taxRateApiClient = taxRateApiClient;
            _logger = logger;
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
                var rate = await _taxRateApiClient.GetCurrentRateAsync();
                return Ok(new { rate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching current tax rate");
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { error = "Failed to fetch tax rate" });
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
                if (updateRequest == null || updateRequest.Rate < 0)
                {
                    return BadRequest(new { error = "Invalid rate value" });
                }

                var result = await _taxRateApiClient.UpdateRateAsync(updateRequest.Rate);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized tax rate update attempt");
                return Forbid();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error updating tax rate");
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
