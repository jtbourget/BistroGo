using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Mvc;

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

        public PickupSlotsController(IPickupTimeSlotRepository repository)
        {
            _repository = repository;
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
            var created = await _repository.AddAsync(slot);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
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
    }
}
