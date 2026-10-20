using System.Net.Http.Json;
using BistroGo.Core.Models;

namespace BistroGo.Web.Services
{
    /// <summary>
    /// Typed HTTP client for interacting with the Pickup Slots API.
    /// </summary>
    public class PickupSlotApiClient
    {
        private readonly HttpClient _client;

        public PickupSlotApiClient(HttpClient client)
        {
            _client = client;
        }

        /// <summary>Calls GET api/pickupslots to retrieve pickup time slots.</summary>
        public async Task<IEnumerable<PickupTimeSlot>> GetPickupSlotsAsync(string? date = null)
        {
            var url = string.IsNullOrEmpty(date) ? "api/pickupslots" : $"api/pickupslots?date={date}";
            var slots = await _client.GetFromJsonAsync<IEnumerable<PickupTimeSlot>>(url);
            return slots ?? Enumerable.Empty<PickupTimeSlot>();
        }

        /// <summary>Calls GET api/pickupslots/{id} to retrieve a slot by identifier.</summary>
        public async Task<PickupTimeSlot?> GetPickupSlotByIdAsync(int id)
        {
            var response = await _client.GetAsync($"api/pickupslots/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PickupTimeSlot>();
        }

        /// <summary>Calls POST api/pickupslots to create a pickup slot.</summary>
        public async Task<PickupTimeSlot?> CreatePickupSlotAsync(PickupTimeSlot slot)
        {
            var response = await _client.PostAsJsonAsync("api/pickupslots", slot);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PickupTimeSlot>();
            }

            return null;
        }

        /// <summary>Calls PUT api/pickupslots/{id} to update a pickup slot.</summary>
        public async Task<PickupTimeSlot?> UpdatePickupSlotAsync(int id, PickupTimeSlot slot)
        {
            var response = await _client.PutAsJsonAsync($"api/pickupslots/{id}", slot);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PickupTimeSlot>();
            }

            return null;
        }

        /// <summary>Calls DELETE api/pickupslots/{id} to soft-delete a pickup slot.</summary>
        public async Task<bool> DeletePickupSlotAsync(int id)
        {
            var response = await _client.DeleteAsync($"api/pickupslots/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
