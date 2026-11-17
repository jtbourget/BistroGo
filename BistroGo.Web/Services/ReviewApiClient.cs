using System.Net.Http.Json;
using BistroGo.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Web.Services
{
    /// <summary>
    /// Typed HTTP client for interacting with the Reviews API.
    /// </summary>
    public class ReviewApiClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<ReviewApiClient> _logger;

        public ReviewApiClient(HttpClient client, ILogger<ReviewApiClient>? logger = null)
        {
            _client = client;
            _logger = logger ?? NullLogger<ReviewApiClient>.Instance;
        }

        /// <summary>Calls GET api/reviews to retrieve all reviews.</summary>
        public async Task<IEnumerable<Review>> GetReviewsAsync()
        {
            try
            {
                _logger.LogDebug("Calling GET api/reviews");
                var reviews = await _client.GetFromJsonAsync<IEnumerable<Review>>("api/reviews");
                return reviews ?? Enumerable.Empty<Review>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching reviews from API");
                throw;
            }
        }

        /// <summary>Calls GET api/reviews/{id} to retrieve a review by identifier.</summary>
        public async Task<Review?> GetReviewByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Calling GET api/reviews/{ReviewId}", id);
                var response = await _client.GetAsync($"api/reviews/{id}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Review {ReviewId} not found via API", id);
                    return null;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Review>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching review {ReviewId} from API", id);
                throw;
            }
        }

        /// <summary>Calls GET api/reviews/by-customer/{customerId} to retrieve reviews by customer.</summary>
        public async Task<IEnumerable<Review>> GetReviewsByCustomerIdAsync(string customerId)
        {
            try
            {
                _logger.LogDebug("Calling GET api/reviews/by-customer/{CustomerId}", customerId);
                var reviews = await _client.GetFromJsonAsync<IEnumerable<Review>>($"api/reviews/by-customer/{customerId}");
                return reviews ?? Enumerable.Empty<Review>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching reviews for customer {CustomerId} from API", customerId);
                throw;
            }
        }

        /// <summary>Calls POST api/reviews to submit a new review.</summary>
        public async Task<Review?> CreateReviewAsync(Review review)
        {
            try
            {
                _logger.LogInformation("Calling POST api/reviews for customer {CustomerName}, Rating: {Rating}",
                    review?.CustomerName, review?.Rating);
                var response = await _client.PostAsJsonAsync("api/reviews", review);
                if (response.IsSuccessStatusCode)
                {
                    var created = await response.Content.ReadFromJsonAsync<Review>();
                    _logger.LogInformation("Created review {ReviewId} via API", created?.Id);
                    return created;
                }

                _logger.LogWarning("Failed to create review via API. Status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating review via API");
                throw;
            }
        }

        /// <summary>Calls PUT api/reviews/{id} to update an existing review.</summary>
        public async Task<Review?> UpdateReviewAsync(int id, Review review)
        {
            try
            {
                _logger.LogInformation("Calling PUT api/reviews/{ReviewId}", id);
                var response = await _client.PutAsJsonAsync($"api/reviews/{id}", review);
                if (response.IsSuccessStatusCode)
                {
                    var updated = await response.Content.ReadFromJsonAsync<Review>();
                    _logger.LogInformation("Updated review {ReviewId} via API", id);
                    return updated;
                }

                _logger.LogWarning("Failed to update review {ReviewId} via API. Status: {StatusCode}", id, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating review {ReviewId} via API", id);
                throw;
            }
        }

        /// <summary>Calls DELETE api/reviews/{id} to soft-delete a review.</summary>
        public async Task<bool> DeleteReviewAsync(int id)
        {
            try
            {
                _logger.LogInformation("Calling DELETE api/reviews/{ReviewId}", id);
                var response = await _client.DeleteAsync($"api/reviews/{id}");
                var success = response.IsSuccessStatusCode;
                if (!success)
                {
                    _logger.LogWarning("Failed to delete review {ReviewId} via API. Status: {StatusCode}", id, response.StatusCode);
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting review {ReviewId} via API", id);
                throw;
            }
        }

        /// <summary>Calls POST api/reviews/{id}/comments to add a comment to a review.</summary>
        public async Task<ReviewComment?> AddCommentAsync(int reviewId, ReviewComment comment)
        {
            try
            {
                _logger.LogInformation("Calling POST api/reviews/{ReviewId}/comments", reviewId);
                var response = await _client.PostAsJsonAsync($"api/reviews/{reviewId}/comments", comment);
                if (response.IsSuccessStatusCode)
                {
                    var created = await response.Content.ReadFromJsonAsync<ReviewComment>();
                    _logger.LogInformation("Added comment {CommentId} to review {ReviewId} via API", created?.Id, reviewId);
                    return created;
                }

                _logger.LogWarning("Failed to add comment to review {ReviewId} via API. Status: {StatusCode}", reviewId, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding comment to review {ReviewId} via API", reviewId);
                throw;
            }
        }

        /// <summary>Calls GET api/reviews/{id}/comments to retrieve comments for a review.</summary>
        public async Task<IEnumerable<ReviewComment>> GetCommentsAsync(int reviewId)
        {
            try
            {
                _logger.LogDebug("Calling GET api/reviews/{ReviewId}/comments", reviewId);
                var comments = await _client.GetFromJsonAsync<IEnumerable<ReviewComment>>($"api/reviews/{reviewId}/comments");
                return comments ?? Enumerable.Empty<ReviewComment>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching comments for review {ReviewId} from API", reviewId);
                throw;
            }
        }
    }
}
