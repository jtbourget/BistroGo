using System.Net.Http.Json;
using BistroGo.Core.Models;

namespace BistroGo.Web.Services
{
    /// <summary>
    /// Typed HTTP client for interacting with the Reviews API.
    /// </summary>
    public class ReviewApiClient
    {
        private readonly HttpClient _client;

        public ReviewApiClient(HttpClient client)
        {
            _client = client;
        }

        /// <summary>Calls GET api/reviews to retrieve customer reviews.</summary>
        public async Task<IEnumerable<Review>> GetReviewsAsync()
        {
            var reviews = await _client.GetFromJsonAsync<IEnumerable<Review>>("api/reviews");
            return reviews ?? Enumerable.Empty<Review>();
        }

        /// <summary>Calls GET api/reviews/{id} to retrieve a review by identifier.</summary>
        public async Task<Review?> GetReviewByIdAsync(int id)
        {
            var response = await _client.GetAsync($"api/reviews/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Review>();
        }

        /// <summary>Calls GET api/reviews/by-customer/{customerId} to retrieve reviews by customer ID.</summary>
        public async Task<IEnumerable<Review>> GetReviewsByCustomerIdAsync(string customerId)
        {
            var reviews = await _client.GetFromJsonAsync<IEnumerable<Review>>($"api/reviews/by-customer/{customerId}");
            return reviews ?? Enumerable.Empty<Review>();
        }

        /// <summary>Calls POST api/reviews to submit a new review.</summary>
        public async Task<Review?> CreateReviewAsync(Review review)
        {
            var response = await _client.PostAsJsonAsync("api/reviews", review);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Review>();
            }

            return null;
        }

        /// <summary>Calls PUT api/reviews/{id} to update an existing review.</summary>
        public async Task<Review?> UpdateReviewAsync(int id, Review review)
        {
            var response = await _client.PutAsJsonAsync($"api/reviews/{id}", review);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Review>();
            }

            return null;
        }

        /// <summary>Calls DELETE api/reviews/{id} to soft-delete a review.</summary>
        public async Task<bool> DeleteReviewAsync(int id)
        {
            var response = await _client.DeleteAsync($"api/reviews/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
