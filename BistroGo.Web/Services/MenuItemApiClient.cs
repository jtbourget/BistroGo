using System.Net.Http.Json;
using BistroGo.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Web.Services
{
    /// <summary>
    /// Typed HTTP client for interacting with the Menu Items API.
    /// </summary>
    public class MenuItemApiClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<MenuItemApiClient> _logger;

        public MenuItemApiClient(HttpClient client, ILogger<MenuItemApiClient>? logger = null)
        {
            _client = client;
            _logger = logger ?? NullLogger<MenuItemApiClient>.Instance;
        }

        /// <summary>Calls GET api/menuitems to retrieve all active menu items.</summary>
        public async Task<IEnumerable<MenuItem>> GetMenuItemsAsync()
        {
            try
            {
                _logger.LogDebug("Calling GET api/menuitems");
                var items = await _client.GetFromJsonAsync<IEnumerable<MenuItem>>("api/menuitems");
                return items ?? Enumerable.Empty<MenuItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching menu items from API");
                throw;
            }
        }

        /// <summary>Calls GET api/menuitems/{id} to retrieve a menu item by identifier.</summary>
        public async Task<MenuItem?> GetMenuItemByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Calling GET api/menuitems/{MenuItemId}", id);
                var response = await _client.GetAsync($"api/menuitems/{id}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Menu item {MenuItemId} not found via API", id);
                    return null;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<MenuItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching menu item {MenuItemId} from API", id);
                throw;
            }
        }

        /// <summary>Calls POST api/menuitems to create a new menu item.</summary>
        public async Task<MenuItem?> CreateMenuItemAsync(MenuItem item)
        {
            try
            {
                _logger.LogInformation("Calling POST api/menuitems for {MenuItemName}", item?.Name);
                var response = await _client.PostAsJsonAsync("api/menuitems", item);
                if (response.IsSuccessStatusCode)
                {
                    var created = await response.Content.ReadFromJsonAsync<MenuItem>();
                    _logger.LogInformation("Created menu item {MenuItemId} via API", created?.Id);
                    return created;
                }

                _logger.LogWarning("Failed to create menu item via API. Status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating menu item {MenuItemName} via API", item?.Name);
                throw;
            }
        }

        /// <summary>Calls PUT api/menuitems/{id} to update an existing menu item.</summary>
        public async Task<MenuItem?> UpdateMenuItemAsync(int id, MenuItem item)
        {
            try
            {
                _logger.LogInformation("Calling PUT api/menuitems/{MenuItemId}", id);
                var response = await _client.PutAsJsonAsync($"api/menuitems/{id}", item);
                if (response.IsSuccessStatusCode)
                {
                    var updated = await response.Content.ReadFromJsonAsync<MenuItem>();
                    _logger.LogInformation("Updated menu item {MenuItemId} via API", id);
                    return updated;
                }

                _logger.LogWarning("Failed to update menu item {MenuItemId} via API. Status: {StatusCode}", id, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating menu item {MenuItemId} via API", id);
                throw;
            }
        }

        /// <summary>Calls DELETE api/menuitems/{id} to soft-delete a menu item.</summary>
        public async Task<bool> DeleteMenuItemAsync(int id)
        {
            try
            {
                _logger.LogInformation("Calling DELETE api/menuitems/{MenuItemId}", id);
                var response = await _client.DeleteAsync($"api/menuitems/{id}");
                var success = response.IsSuccessStatusCode;
                if (!success)
                {
                    _logger.LogWarning("Failed to delete menu item {MenuItemId} via API. Status: {StatusCode}", id, response.StatusCode);
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting menu item {MenuItemId} via API", id);
                throw;
            }
        }

        /// <summary>Calls GET api/categories to retrieve category options for menu item creation.</summary>
        public async Task<IEnumerable<MenuCategory>> GetCategoriesAsync()
        {
            try
            {
                _logger.LogDebug("Calling GET api/categories for menu options");
                var categories = await _client.GetFromJsonAsync<IEnumerable<MenuCategory>>("api/categories");
                return categories ?? Enumerable.Empty<MenuCategory>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching categories from API");
                throw;
            }
        }
    }
}
