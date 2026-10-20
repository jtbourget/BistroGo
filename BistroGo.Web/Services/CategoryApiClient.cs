using System.Net.Http.Json;
using BistroGo.Core.Models;

namespace BistroGo.Web.Services
{
    /// <summary>
    /// Typed HTTP client for interacting with the Categories API.
    /// </summary>
    public class CategoryApiClient
    {
        private readonly HttpClient _client;

        public CategoryApiClient(HttpClient client)
        {
            _client = client;
        }

        /// <summary>Calls GET api/categories to retrieve all active categories.</summary>
        public async Task<IEnumerable<MenuCategory>> GetCategoriesAsync()
        {
            var categories = await _client.GetFromJsonAsync<IEnumerable<MenuCategory>>("api/categories");
            return categories ?? Enumerable.Empty<MenuCategory>();
        }

        /// <summary>Calls GET api/categories/{id} to retrieve a category by identifier.</summary>
        public async Task<MenuCategory?> GetCategoryByIdAsync(int id)
        {
            var response = await _client.GetAsync($"api/categories/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MenuCategory>();
        }

        /// <summary>Calls POST api/categories to create a new category.</summary>
        public async Task<MenuCategory?> CreateCategoryAsync(MenuCategory category)
        {
            var response = await _client.PostAsJsonAsync("api/categories", category);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MenuCategory>();
            }

            return null;
        }

        /// <summary>Calls PUT api/categories/{id} to update an existing category.</summary>
        public async Task<MenuCategory?> UpdateCategoryAsync(int id, MenuCategory category)
        {
            var response = await _client.PutAsJsonAsync($"api/categories/{id}", category);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MenuCategory>();
            }

            return null;
        }

        /// <summary>Calls DELETE api/categories/{id} to soft-delete a category.</summary>
        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var response = await _client.DeleteAsync($"api/categories/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
