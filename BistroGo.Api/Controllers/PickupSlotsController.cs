using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Mvc;
using BistroGo.Api.Contracts;
using BistroGo.Core.Enumerations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Api.Controllers
{
    /// <summary>
    /// RESTful API controller for managing customer pickup time slots.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/pickuptimeslots")]
    public class PickupSlotsController : ControllerBase
    {
        private readonly IPickupTimeSlotRepository _repository;
        private readonly IPickupSlotService _service;
        private readonly ILogger<PickupSlotsController> _logger;

        public PickupSlotsController(
            IPickupTimeSlotRepository repository,
            IPickupSlotService service,
            ILogger<PickupSlotsController>? logger = null)
        {
            _repository = repository;
            _service = service;
            _logger = logger ?? NullLogger<PickupSlotsController>.Instance;
        }

        /// <summary>Retrieves all active pickup slots, optionally filtered by date.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PickupTimeSlot>>> Get([FromQuery] string? date = null)
        {
            if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var slotDate))
            {
                _logger.LogInformation("Retrieving pickup slots for date {SlotDate}", slotDate);
                var slotsByDate = await _repository.GetByDateAsync(slotDate);
                return Ok(slotsByDate);
            }

            _logger.LogInformation("Retrieving all active pickup slots");
            var slots = await _repository.GetAllAsync();
            return Ok(slots);
        }

        /// <summary>Retrieves a pickup slot by identifier.</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PickupTimeSlot>> GetById(int id)
        {
            _logger.LogInformation("Retrieving pickup slot by id {PickupSlotId}", id);
            var slot = await _repository.GetByIdAsync(id);
            if (slot == null)
            {
                _logger.LogWarning("Pickup slot {PickupSlotId} not found", id);
                return NotFound();
            }
            return Ok(slot);
        }

        /// <summary>Creates a new pickup time slot.</summary>
        [HttpPost]
        public async Task<ActionResult<PickupTimeSlot>> Post([FromBody] PickupTimeSlot slot)
        {
            _logger.LogInformation("Creating pickup time slot on {SlotDate} from {StartTime} to {EndTime}, MaxOrders: {MaxOrders}",
                slot.SlotDate, slot.StartTime, slot.EndTime, slot.MaxOrders);

            var outcome = await _service.CreateAsync(slot);

            switch (outcome.Result)
            {
                case CreateSlotResult.Created:
                    _logger.LogInformation("Successfully created pickup slot {PickupSlotId}", outcome.Slot!.Id);
                    return CreatedAtAction(nameof(GetById), new { id = outcome.Slot!.Id }, outcome.Slot);
                case CreateSlotResult.Duplicate:
                    _logger.LogWarning("Failed to create pickup slot: duplicate slot for date {SlotDate} and time {StartTime}", slot.SlotDate, slot.StartTime);
                    return Conflict(new ProblemDetails { Title = "A slot already exists for that date and start time.", Status = StatusCodes.Status409Conflict });
                case CreateSlotResult.DateInPast:
                    _logger.LogWarning("Failed to create pickup slot: date {SlotDate} is in the past", slot.SlotDate);
                    return BadRequestProblem("Slots can't be created for a date in the past.");
                case CreateSlotResult.EndNotAfterStart:
                    _logger.LogWarning("Failed to create pickup slot: end time {EndTime} not after start time {StartTime}", slot.EndTime, slot.StartTime);
                    return BadRequestProblem("The end time must be after the start time.");
                case CreateSlotResult.InvalidCapacity:
                    _logger.LogWarning("Failed to create pickup slot: invalid capacity {MaxOrders}", slot.MaxOrders);
                    return BadRequestProblem("Max orders must be at least 1.");
                default:
                    _logger.LogError("Unhandled create slot result {Result}", outcome.Result);
                    throw new InvalidOperationException($"Unhandled result: {outcome.Result}.");
            }
        }

        /// <summary>Updates an existing pickup time slot.</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<PickupTimeSlot>> Put(int id, [FromBody] PickupTimeSlot slot)
        {
            _logger.LogInformation("Updating pickup slot {PickupSlotId}", id);
            slot.Id = id;
            var updated = await _repository.UpdateAsync(slot);
            if (!updated)
            {
                _logger.LogWarning("Failed to update pickup slot {PickupSlotId}: not found", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully updated pickup slot {PickupSlotId}", id);
            return Ok(slot);
        }

        /// <summary>Soft-deletes a pickup slot by identifier.</summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            _logger.LogInformation("Deleting pickup slot {PickupSlotId}", id);
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogWarning("Failed to delete pickup slot {PickupSlotId}: not found", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully deleted pickup slot {PickupSlotId}", id);
            return NoContent();
        }

        /// <summary>Slots a customer can still book on a date (default today), with spots remaining.</summary>
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<AvailableSlotResponse>>> GetAvailable([FromQuery] DateOnly? date = null)
        {
            _logger.LogInformation("Retrieving available pickup slots for date {Date}", date);
            var available = await _service.GetAvailableAsync(date);

            return Ok(available.Select(a => new AvailableSlotResponse(
                a.Slot.Id, a.Slot.SlotDate, a.Slot.StartTime, a.Slot.EndTime, a.Slot.MaxOrders, a.Remaining)));
        }

        /// <summary>Creates a day's slots in one request. Slots that already exist are skipped.</summary>
        [HttpPost("bulk")]
        public async Task<ActionResult<BulkSlotsResponse>> PostBulk([FromBody] BulkSlotRequest request)
        {
            _logger.LogInformation("Creating bulk pickup slots for date {Date}, Opens: {Opens}, Closes: {Closes}, Interval: {Interval}min",
                request.Date, request.Opens, request.Closes, request.SlotMinutes);

            var outcome = await _service.CreateDayAsync(request);

            switch (outcome.Result)
            {
                case BulkSlotResult.Created:
                    _logger.LogInformation("Bulk slot creation succeeded: {CreatedCount} created, {SkippedCount} skipped",
                        outcome.Created.Count, outcome.Skipped);
                    return Ok(new BulkSlotsResponse(outcome.Created.Count, outcome.Skipped, outcome.Created));
                case BulkSlotResult.DateInPast:
                    _logger.LogWarning("Bulk slot creation failed: date {Date} in past", request.Date);
                    return BadRequestProblem("Slots can't be created for a date in the past.");
                case BulkSlotResult.InvalidHours:
                    _logger.LogWarning("Bulk slot creation failed: closing time {Closes} must be after opening {Opens}", request.Closes, request.Opens);
                    return BadRequestProblem("Closing time must be after opening time.");
                case BulkSlotResult.InvalidSlotLength:
                    _logger.LogWarning("Bulk slot creation failed: invalid slot length {SlotMinutes}", request.SlotMinutes);
                    return BadRequestProblem("Slot length must be between 5 and 240 minutes.");
                case BulkSlotResult.InvalidCapacity:
                    _logger.LogWarning("Bulk slot creation failed: invalid capacity {MaxOrders}", request.MaxOrders);
                    return BadRequestProblem("Max orders must be at least 1.");
                default:
                    _logger.LogError("Unhandled bulk slot result {Result}", outcome.Result);
                    throw new InvalidOperationException($"Unhandled result: {outcome.Result}.");
            }
        }

        private BadRequestObjectResult BadRequestProblem(string title) =>
            BadRequest(new ProblemDetails { Title = title, Status = StatusCodes.Status400BadRequest });
    }
}
