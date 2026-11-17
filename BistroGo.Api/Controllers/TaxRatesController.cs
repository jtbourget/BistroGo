using BistroGo.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Api.Controllers
{
    /// <summary>
    /// API controller for tax rate management.
    /// Provides endpoints for retrieving and updating the current tax rate.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TaxRatesController : ControllerBase
    {
        private readonly ITaxRateService _taxRateService;
        private readonly ILogger<TaxRatesController> _logger;

        public TaxRatesController(ITaxRateService taxRateService, ILogger<TaxRatesController>? logger = null)
        {
            _taxRateService = taxRateService;
            _logger = logger ?? NullLogger<TaxRatesController>.Instance;
        }

        /// <summary>
        /// Retrieves the current active tax rate.
        /// </summary>
        /// <returns>The current tax rate as a decimal (e.g., 0.08 for 8%)</returns>
        [HttpGet("current")]
        public IActionResult GetCurrentRate()
        {
            _logger.LogInformation("Retrieving current tax rate");
            var rate = _taxRateService.GetCurrentRate();
            _logger.LogInformation("Current tax rate is {Rate}", rate);
            return Ok(new { rate });
        }

        /// <summary>
        /// Updates the current tax rate.
        /// Authorization (Manager role check) is enforced by the Web proxy controller.
        /// The API key provides service-to-service authentication.
        /// </summary>
        /// <param name="updateRequest">Object containing the new rate</param>
        /// <returns>Success/failure response</returns>
        [HttpPost("update")]
        public IActionResult UpdateRate([FromBody] UpdateTaxRateRequest updateRequest)
        {
            if (updateRequest == null || updateRequest.Rate < 0)
            {
                _logger.LogWarning("Invalid tax rate update request received");
                return BadRequest(new { error = "Invalid rate value" });
            }

            _logger.LogInformation("Attempting to update tax rate to {NewRate}", updateRequest.Rate);
            bool success = _taxRateService.UpdateRate(updateRequest.Rate);
            if (!success)
            {
                _logger.LogWarning("Tax rate update rejected: {Rate} is outside permitted 0%-20% bounds", updateRequest.Rate);
                return BadRequest(new { error = "Rate must be between 0% and 20%" });
            }

            _logger.LogInformation("Tax rate successfully updated to {NewRate}", updateRequest.Rate);
            return Ok(new { message = "Tax rate updated successfully", newRate = updateRequest.Rate });
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
