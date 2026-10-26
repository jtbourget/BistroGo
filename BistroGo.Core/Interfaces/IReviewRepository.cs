using BistroGo.Core.Models;

namespace BistroGo.Core.Interfaces
{
    /// <summary>
    /// Contract for managing customer reviews and feedback.
    /// </summary>
    public interface IReviewRepository
    {
        /// <summary>Retrieves all non-archived customer reviews ordered by submission date descending.</summary>
        Task<List<Review>> GetAllAsync();

        /// <summary>Retrieves a single review by identifier.</summary>
        Task<Review?> GetByIdAsync(int id);

        /// <summary>Retrieves all reviews authored by a specific customer account.</summary>
        Task<List<Review>> GetByCustomerIdAsync(string customerId);

        /// <summary>Persists a new customer review.</summary>
        Task<Review> AddAsync(Review review);

        /// <summary>Updates review ratings and review text.</summary>
        Task<bool> UpdateAsync(Review review);

        /// <summary>Soft-deletes a review by setting IsArchived = true.</summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>Adds a comment to an existing review.</summary>
        Task<ReviewComment> AddCommentAsync(ReviewComment comment);

        /// <summary>Retrieves all comments for a specific review.</summary>
        Task<List<ReviewComment>> GetCommentsByReviewIdAsync(int reviewId);
    }
}
