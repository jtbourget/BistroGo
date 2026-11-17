using System.Net.Http.Json;
using BistroGo.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Web.Services
{
    /// <summary>
    /// Typed HTTP client for interacting with the Categories API.
    /// </summary>
    public class CategoryApiClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<CategoryApiClient> _logger;

        public CategoryApiClient(HttpClient client, ILogger<CategoryApiClient>? logger = null)
        {
            _client = client;
            _logger = logger ?? NullLogger<CategoryApiClient>.Instance;
        }

        /// <summary>Calls GET api/categories to retrieve all active categories.</summary>
        public async Task<IEnumerable<MenuCategory>> GetCategoriesAsync()
        {
            try
            {
                _logger.LogDebug("Calling GET api/categories");
                var categories = await _client.GetFromJsonAsync<IEnumerable<MenuCategory>>("api/categories");
                return categories ?? Enumerable.Empty<MenuCategory>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching categories from API");
                throw;
            }
        }

        /// <summary>Calls GET api/categories/{id} to retrieve a category by identifier.</summary>
        public async Task<MenuCategory?> GetCategoryByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Calling GET api/categories/{CategoryId}", id);
                var response = await _client.GetAsync($"api/categories/{id}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Category {CategoryId} not found via API", id);
                    return null;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MenuCategory>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching category {CategoryId} from API", id);
                throw;
            }
        }

        /// <summary>Calls POST api/categories to create a new category.</summary>
        public async Task<MenuCategory?> CreateCategoryAsync(MenuCategory category)
        {
            try
            {
                _logger.LogInformation("Calling POST api/categories for {CategoryName}", category?.Name);
                var response = await _client.PostAsJsonAsync("api/categories", category);
                if (response.IsSuccessStatusCode)
                {
                    var created = await response.Content.ReadFromJsonAsync<MenuCategory>();
                    _logger.LogInformation("Created category {CategoryId} via API", created?.Id);
                    return created;
                }

                _logger.LogWarning("Failed to create category via API. Status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating category {CategoryName} via API", category?.Name);
                throw;
            }
        }

        /// <summary>Calls PUT api/categories/{id} to update an existing category.</summary>
        public async Task<MenuCategory?> UpdateCategoryAsync(int id, MenuCategory category)
        {
            try
            {
                _logger.LogInformation("Calling PUT api/categories/{CategoryId}", id);
                var response = await _client.PutAsJsonAsync($"api/categories/{id}", category);
                if (response.IsSuccessStatusCode)
                {
                    var updated = await response.Content.ReadFromJsonAsync<MenuCategory>();
                    _logger.LogInformation("Updated category {CategoryId} via API", id);
                    return updated;
                }

                _logger.LogWarning("Failed to update category {CategoryId} via API. Status: {StatusCode}", id, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating category {CategoryId} via API", id);
                throw;
            }
        }

        /// <summary>Calls DELETE api/categories/{id} to soft-delete a category.</summary>
        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                _logger.LogInformation("Calling DELETE api/categories/{CategoryId}", id);
                var response = await _client.DeleteAsync($"api/categories/{id}");
                var success = response.IsSuccessStatusCode;
                if (!success)
                {
                    _logger.LogWarning("Failed to delete category {CategoryId} via API. Status: {StatusCode}", id, response.StatusCode);
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting category {CategoryId} via API", id);
                throw;
            }
        }
    }
}
