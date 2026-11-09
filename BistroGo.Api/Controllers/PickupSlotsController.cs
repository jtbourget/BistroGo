using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Mvc;
using BistroGo.Api.Contracts;
using BistroGo.Core.Enumerations;

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

        public PickupSlotsController(IPickupTimeSlotRepository repository, IPickupSlotService service)
        {
            _repository = repository;
            _service = service;
        }

        /// <summary>Retrieves all active pickup slots, optionally filtered by date.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PickupTimeSlot>>> Get([FromQuery] string? date = null)
        {
            if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var slotDate))
            {
                var slotsByDate = await _repository.GetByDateAsync(slotDate);
                return Ok(slotsByDate);
            }

            var slots = await _repository.GetAllAsync();
            return Ok(slots);
        }

        /// <summary>Retrieves a pickup slot by identifier.</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PickupTimeSlot>> GetById(int id)
        {
            var slot = await _repository.GetByIdAsync(id);
            if (slot == null)
            {
                return NotFound();
            }
            return Ok(slot);
        }

        /// <summary>Creates a new pickup time slot.</summary>
        [HttpPost]
        public async Task<ActionResult<PickupTimeSlot>> Post([FromBody] PickupTimeSlot slot)
        {
            var outcome = await _service.CreateAsync(slot);

            return outcome.Result switch
            {
                CreateSlotResult.Created =>
                    CreatedAtAction(nameof(GetById), new { id = outcome.Slot!.Id }, outcome.Slot),
                CreateSlotResult.Duplicate =>
                    Conflict(new ProblemDetails { Title = "A slot already exists for that date and start time.", Status = StatusCodes.Status409Conflict }),
                CreateSlotResult.DateInPast => BadRequestProblem("Slots can't be created for a date in the past."),
                CreateSlotResult.EndNotAfterStart => BadRequestProblem("The end time must be after the start time."),
                CreateSlotResult.InvalidCapacity => BadRequestProblem("Max orders must be at least 1."),
                _ => throw new InvalidOperationException($"Unhandled result: {outcome.Result}.")
            };
        }

        /// <summary>Updates an existing pickup time slot.</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<PickupTimeSlot>> Put(int id, [FromBody] PickupTimeSlot slot)
        {
            slot.Id = id;
            var updated = await _repository.UpdateAsync(slot);
            if (!updated)
            {
                return NotFound();
            }
            return Ok(slot);
        }

        /// <summary>Soft-deletes a pickup slot by identifier.</summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }

        /// <summary>Slots a customer can still book on a date (default today), with spots remaining.</summary>
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<AvailableSlotResponse>>> GetAvailable([FromQuery] DateOnly? date = null)
        {
            var available = await _service.GetAvailableAsync(date);

            return Ok(available.Select(a => new AvailableSlotResponse(
                a.Slot.Id, a.Slot.SlotDate, a.Slot.StartTime, a.Slot.EndTime, a.Slot.MaxOrders, a.Remaining)));
        }

        /// <summary>Creates a day's slots in one request. Slots that already exist are skipped.</summary>
        [HttpPost("bulk")]
        public async Task<ActionResult<BulkSlotsResponse>> PostBulk([FromBody] BulkSlotRequest request)
        {
            var outcome = await _service.CreateDayAsync(request);

            return outcome.Result switch
            {
                BulkSlotResult.Created => Ok(new BulkSlotsResponse(outcome.Created.Count, outcome.Skipped, outcome.Created)),
                BulkSlotResult.DateInPast => BadRequestProblem("Slots can't be created for a date in the past."),
                BulkSlotResult.InvalidHours => BadRequestProblem("Closing time must be after opening time."),
                BulkSlotResult.InvalidSlotLength => BadRequestProblem("Slot length must be between 5 and 240 minutes."),
                BulkSlotResult.InvalidCapacity => BadRequestProblem("Max orders must be at least 1."),
                _ => throw new InvalidOperationException($"Unhandled result: {outcome.Result}.")
            };
        }

        private BadRequestObjectResult BadRequestProblem(string title) =>
            BadRequest(new ProblemDetails { Title = title, Status = StatusCodes.Status400BadRequest });
    }
}
