using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace BistroGo.Api.Controllers
{
    /// <summary>
    /// RESTful API controller for customer reviews and ratings.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewRepository _repository;

        public ReviewsController(IReviewRepository repository)
        {
            _repository = repository;
        }

        /// <summary>Retrieves all customer reviews.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Review>>> Get()
        {
            var reviews = await _repository.GetAllAsync();
            return Ok(reviews);
        }

        /// <summary>Retrieves a review by identifier.</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Review>> GetById(int id)
        {
            var review = await _repository.GetByIdAsync(id);
            if (review == null)
            {
                return NotFound();
            }
            return Ok(review);
        }

        /// <summary>Retrieves all reviews by a specific customer ID.</summary>
        [HttpGet("by-customer/{customerId}")]
        public async Task<ActionResult<IEnumerable<Review>>> GetByCustomerId(string customerId)
        {
            var reviews = await _repository.GetByCustomerIdAsync(customerId);
            return Ok(reviews);
        }

        /// <summary>Submits a new customer review.</summary>
        [HttpPost]
        public async Task<ActionResult<Review>> Post([FromBody] Review review)
        {
            var created = await _repository.AddAsync(review);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Updates an existing review.</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<Review>> Put(int id, [FromBody] Review review)
        {
            review.Id = id;
            var updated = await _repository.UpdateAsync(review);
            if (!updated)
            {
                return NotFound();
            }
            return Ok(review);
        }

        /// <summary>Soft-deletes a review by identifier.</summary>
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
