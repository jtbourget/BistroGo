using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(IReviewRepository repository, ILogger<ReviewsController>? logger = null)
        {
            _repository = repository;
            _logger = logger ?? NullLogger<ReviewsController>.Instance;
        }

        /// <summary>Retrieves all customer reviews.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Review>>> Get()
        {
            _logger.LogInformation("Retrieving all customer reviews");
            var reviews = await _repository.GetAllAsync();
            return Ok(reviews);
        }

        /// <summary>Retrieves a review by identifier.</summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Review>> GetById(int id)
        {
            _logger.LogInformation("Retrieving review by id {ReviewId}", id);
            var review = await _repository.GetByIdAsync(id);
            if (review == null)
            {
                _logger.LogWarning("Review {ReviewId} not found", id);
                return NotFound();
            }
            return Ok(review);
        }

        /// <summary>Retrieves all reviews by a specific customer ID.</summary>
        [HttpGet("by-customer/{customerId}")]
        public async Task<ActionResult<IEnumerable<Review>>> GetByCustomerId(string customerId)
        {
            _logger.LogInformation("Retrieving reviews for customer {CustomerId}", customerId);
            var reviews = await _repository.GetByCustomerIdAsync(customerId);
            return Ok(reviews);
        }

        /// <summary>Submits a new customer review.</summary>
        [HttpPost]
        public async Task<ActionResult<Review>> Post([FromBody] Review review)
        {
            _logger.LogInformation("Submitting customer review, Rating: {Rating}, Customer: {CustomerName}",
                review?.Rating, review?.CustomerName);
            var created = await _repository.AddAsync(review!);
            _logger.LogInformation("Submitted customer review {ReviewId} successfully", created.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>Updates an existing review.</summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<Review>> Put(int id, [FromBody] Review review)
        {
            _logger.LogInformation("Updating review {ReviewId}", id);
            review.Id = id;
            var updated = await _repository.UpdateAsync(review);
            if (!updated)
            {
                _logger.LogWarning("Failed to update review {ReviewId}: not found", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully updated review {ReviewId}", id);
            return Ok(review);
        }

        /// <summary>Soft-deletes a review by identifier.</summary>
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            _logger.LogInformation("Deleting review {ReviewId}", id);
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
            {
                _logger.LogWarning("Failed to delete review {ReviewId}: not found", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully deleted review {ReviewId}", id);
            return NoContent();
        }

        /// <summary>Adds a comment to an existing review.</summary>
        [HttpPost("{id:int}/comments")]
        public async Task<ActionResult<ReviewComment>> PostComment(int id, [FromBody] ReviewComment comment)
        {
            _logger.LogInformation("Adding comment to review {ReviewId}", id);
            var review = await _repository.GetByIdAsync(id);
            if (review == null)
            {
                _logger.LogWarning("Cannot add comment: review {ReviewId} not found", id);
                return NotFound();
            }

            comment.ReviewId = id;
            var created = await _repository.AddCommentAsync(comment);
            _logger.LogInformation("Added comment {CommentId} to review {ReviewId}", created.Id, id);
            return Ok(created);
        }

        /// <summary>Retrieves all comments for a specific review.</summary>
        [HttpGet("{id:int}/comments")]
        public async Task<ActionResult<IEnumerable<ReviewComment>>> GetComments(int id)
        {
            _logger.LogInformation("Retrieving comments for review {ReviewId}", id);
            var review = await _repository.GetByIdAsync(id);
            if (review == null)
            {
                _logger.LogWarning("Cannot retrieve comments: review {ReviewId} not found", id);
                return NotFound();
            }

            var comments = await _repository.GetCommentsByReviewIdAsync(id);
            return Ok(comments);
        }
    }
}
