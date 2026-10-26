using BistroGo.Core.Interfaces;
using BistroGo.Core.Models;
using BistroGo.Data.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace BistroGo.Data.Repositories
{
    public class EfReviewRepository : IReviewRepository
    {
        private readonly BistroGoDBContext _db;

        public EfReviewRepository(BistroGoDBContext db)
        {
            _db = db;
        }

        public async Task<List<Review>> GetAllAsync()
        {
            return await _db.Reviews
                .Include(r => r.Comments.OrderBy(c => c.CreatedAt))
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Review?> GetByIdAsync(int id)
        {
            return await _db.Reviews
                .Include(r => r.Comments.OrderBy(c => c.CreatedAt))
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<Review>> GetByCustomerIdAsync(string customerId)
        {
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
            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();
            return review;
        }

        public async Task<bool> UpdateAsync(Review review)
        {
            var existing = await _db.Reviews.FindAsync(review.Id);
            if (existing == null) return false;

            existing.Rating = review.Rating;
            existing.ReviewText = review.ReviewText;
            existing.CustomerName = review.CustomerName;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _db.Reviews.FindAsync(id);
            if (existing == null) return false;

            existing.IsArchived = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<ReviewComment> AddCommentAsync(ReviewComment comment)
        {
            if (comment.CreatedAt == default)
            {
                comment.CreatedAt = DateTime.UtcNow;
            }
            _db.ReviewComments.Add(comment);
            await _db.SaveChangesAsync();
            return comment;
        }

        public async Task<List<ReviewComment>> GetCommentsByReviewIdAsync(int reviewId)
        {
            return await _db.ReviewComments
                .AsNoTracking()
                .Where(c => c.ReviewId == reviewId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}
