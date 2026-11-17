using System.Net.Http.Json;
using BistroGo.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Web.Services
{
    /// <summary>
    /// Typed HTTP client for interacting with the Pickup Slots API.
    /// </summary>
    public class PickupSlotApiClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<PickupSlotApiClient> _logger;

        public PickupSlotApiClient(HttpClient client, ILogger<PickupSlotApiClient>? logger = null)
        {
            _client = client;
            _logger = logger ?? NullLogger<PickupSlotApiClient>.Instance;
        }

        /// <summary>Calls GET api/pickupslots to retrieve pickup time slots.</summary>
        public async Task<IEnumerable<PickupTimeSlot>> GetPickupSlotsAsync(string? date = null)
        {
            try
            {
                var url = string.IsNullOrEmpty(date) ? "api/pickupslots" : $"api/pickupslots?date={date}";
                _logger.LogDebug("Calling GET {Url}", url);
                var slots = await _client.GetFromJsonAsync<IEnumerable<PickupTimeSlot>>(url);
                return slots ?? Enumerable.Empty<PickupTimeSlot>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching pickup slots from API");
                throw;
            }
        }

        /// <summary>Calls GET api/pickupslots/{id} to retrieve a slot by identifier.</summary>
        public async Task<PickupTimeSlot?> GetPickupSlotByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Calling GET api/pickupslots/{SlotId}", id);
                var response = await _client.GetAsync($"api/pickupslots/{id}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Pickup slot {SlotId} not found via API", id);
                    return null;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<PickupTimeSlot>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching pickup slot {SlotId} from API", id);
                throw;
            }
        }

        /// <summary>Calls POST api/pickupslots to create a pickup slot.</summary>
        public async Task<PickupTimeSlot?> CreatePickupSlotAsync(PickupTimeSlot slot)
        {
            try
            {
                _logger.LogInformation("Calling POST api/pickupslots for date {SlotDate} {StartTime}", slot?.SlotDate, slot?.StartTime);
                var response = await _client.PostAsJsonAsync("api/pickupslots", slot);
                if (response.IsSuccessStatusCode)
                {
                    var created = await response.Content.ReadFromJsonAsync<PickupTimeSlot>();
                    _logger.LogInformation("Created pickup slot {SlotId} via API", created?.Id);
                    return created;
                }

                _logger.LogWarning("Failed to create pickup slot via API. Status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating pickup slot via API");
                throw;
            }
        }

        /// <summary>Calls PUT api/pickupslots/{id} to update an existing slot.</summary>
        public async Task<PickupTimeSlot?> UpdatePickupSlotAsync(int id, PickupTimeSlot slot)
        {
            try
            {
                _logger.LogInformation("Calling PUT api/pickupslots/{SlotId}", id);
                var response = await _client.PutAsJsonAsync($"api/pickupslots/{id}", slot);
                if (response.IsSuccessStatusCode)
                {
                    var updated = await response.Content.ReadFromJsonAsync<PickupTimeSlot>();
                    _logger.LogInformation("Updated pickup slot {SlotId} via API", id);
                    return updated;
                }

                _logger.LogWarning("Failed to update pickup slot {SlotId} via API. Status: {StatusCode}", id, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating pickup slot {SlotId} via API", id);
                throw;
            }
        }

        /// <summary>Calls DELETE api/pickupslots/{id} to soft-delete a pickup slot.</summary>
        public async Task<bool> DeletePickupSlotAsync(int id)
        {
            try
            {
                _logger.LogInformation("Calling DELETE api/pickupslots/{SlotId}", id);
                var response = await _client.DeleteAsync($"api/pickupslots/{id}");
                var success = response.IsSuccessStatusCode;
                if (!success)
                {
                    _logger.LogWarning("Failed to delete pickup slot {SlotId} via API. Status: {StatusCode}", id, response.StatusCode);
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting pickup slot {SlotId} via API", id);
                throw;
            }
        }
    }
}
