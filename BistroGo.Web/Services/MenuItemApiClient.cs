using System.Net.Http.Json;
using BistroGo.Core.Models;

namespace BistroGo.Web.Services
{
    /// <summary>
    /// Typed HTTP client for interacting with the Menu Items API.
    /// </summary>
    public class MenuItemApiClient
    {
        private readonly HttpClient _client;

        public MenuItemApiClient(HttpClient client)
        {
            _client = client;
        }

        /// <summary>Calls GET api/menuitems to retrieve all active menu items.</summary>
        public async Task<IEnumerable<MenuItem>> GetMenuItemsAsync()
        {
            var items = await _client.GetFromJsonAsync<IEnumerable<MenuItem>>("api/menuitems");
            return items ?? Enumerable.Empty<MenuItem>();
        }

        /// <summary>Calls GET api/menuitems/{id} to retrieve a menu item by identifier.</summary>
        public async Task<MenuItem?> GetMenuItemByIdAsync(int id)
        {
            var response = await _client.GetAsync($"api/menuitems/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<MenuItem>();
        }

        /// <summary>Calls POST api/menuitems to create a new menu item.</summary>
        public async Task<MenuItem?> CreateMenuItemAsync(MenuItem item)
        {
            var response = await _client.PostAsJsonAsync("api/menuitems", item);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MenuItem>();
            }

            return null;
        }

        /// <summary>Calls PUT api/menuitems/{id} to update an existing menu item.</summary>
        public async Task<MenuItem?> UpdateMenuItemAsync(int id, MenuItem item)
        {
            var response = await _client.PutAsJsonAsync($"api/menuitems/{id}", item);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MenuItem>();
            }

            return null;
        }

        /// <summary>Calls DELETE api/menuitems/{id} to soft-delete a menu item.</summary>
        public async Task<bool> DeleteMenuItemAsync(int id)
        {
            var response = await _client.DeleteAsync($"api/menuitems/{id}");
            return response.IsSuccessStatusCode;
        }

        /// <summary>Calls GET api/categories to retrieve category options for menu item creation.</summary>
        public async Task<IEnumerable<MenuCategory>> GetCategoriesAsync()
        {
            var categories = await _client.GetFromJsonAsync<IEnumerable<MenuCategory>>("api/categories");
            return categories ?? Enumerable.Empty<MenuCategory>();
        }
    }
}
