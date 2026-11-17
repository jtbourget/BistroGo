using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Data.Repositories
{
    public class EfReviewRepository : IReviewRepository
    {
        private readonly BistroGoDBContext _db;
        private readonly ILogger<EfReviewRepository> _logger;

        public EfReviewRepository(BistroGoDBContext db, ILogger<EfReviewRepository>? logger = null)
        {
            _db = db;
            _logger = logger ?? NullLogger<EfReviewRepository>.Instance;
        }

        public async Task<List<Review>> GetAllAsync()
        {
            _logger.LogDebug("Fetching all reviews");
            return await _db.Reviews
                .Include(r => r.Comments.OrderBy(c => c.CreatedAt))
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Review?> GetByIdAsync(int id)
        {
            _logger.LogDebug("Fetching review with id {ReviewId}", id);
            return await _db.Reviews
                .Include(r => r.Comments.OrderBy(c => c.CreatedAt))
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<Review>> GetByCustomerIdAsync(string customerId)
        {
            _logger.LogDebug("Fetching reviews for customer {CustomerId}", customerId);
            return await _db.Reviews
                .Include(r => r.Comments.OrderBy(c => c.CreatedAt))
                .AsNoTracking()
                .Where(r => r.CustomerId == customerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Review> AddAsync(Review review)
        {
            if (review.CreatedAt == default)
            {
                review.CreatedAt = DateTime.UtcNow;
            }
            _logger.LogInformation("Adding review by {CustomerName}, Rating: {Rating}", review.CustomerName, review.Rating);
            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Added review with id {ReviewId}", review.Id);
            return review;
        }

        public async Task<bool> UpdateAsync(Review review)
        {
            _logger.LogInformation("Updating review {ReviewId}", review.Id);
            var existing = await _db.Reviews.FindAsync(review.Id);
            if (existing == null)
            {
                _logger.LogWarning("Cannot update review {ReviewId}: not found", review.Id);
                return false;
            }

            existing.Rating = review.Rating;
            existing.ReviewText = review.ReviewText;
            existing.CustomerName = review.CustomerName;

            await _db.SaveChangesAsync();
            _logger.LogInformation("Successfully updated review {ReviewId}", review.Id);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Soft-deleting review {ReviewId}", id);
            var existing = await _db.Reviews.FindAsync(id);
            if (existing == null)
            {
                _logger.LogWarning("Cannot delete review {ReviewId}: not found", id);
                return false;
            }

            existing.IsArchived = true;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Successfully soft-deleted review {ReviewId}", id);
            return true;
        }

        public async Task<ReviewComment> AddCommentAsync(ReviewComment comment)
        {
            if (comment.CreatedAt == default)
            {
                comment.CreatedAt = DateTime.UtcNow;
            }
            _logger.LogInformation("Adding comment to review {ReviewId} by {ManagerName}", comment.ReviewId, comment.ManagerName);
            _db.ReviewComments.Add(comment);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Added comment with id {CommentId} to review {ReviewId}", comment.Id, comment.ReviewId);
            return comment;
        }

        public async Task<List<ReviewComment>> GetCommentsByReviewIdAsync(int reviewId)
        {
            _logger.LogDebug("Fetching comments for review {ReviewId}", reviewId);
            return await _db.ReviewComments
                .AsNoTracking()
                .Where(c => c.ReviewId == reviewId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}
