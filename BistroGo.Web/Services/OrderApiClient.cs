using System.Net.Http.Json;
using BistroGo.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Web.Services
{
    /// <summary>
    /// Typed HTTP client for interacting with the Orders API.
    /// </summary>
    public class OrderApiClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<OrderApiClient> _logger;

        public OrderApiClient(HttpClient client, ILogger<OrderApiClient>? logger = null)
        {
            _client = client;
            _logger = logger ?? NullLogger<OrderApiClient>.Instance;
        }

        /// <summary>Calls GET api/orders to retrieve all orders.</summary>
        public async Task<IEnumerable<Order>> GetOrdersAsync()
        {
            try
            {
                _logger.LogDebug("Calling GET api/orders");
                var orders = await _client.GetFromJsonAsync<IEnumerable<Order>>("api/orders");
                return orders ?? Enumerable.Empty<Order>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching orders from API");
                throw;
            }
        }

        /// <summary>Calls GET api/orders/{id} to retrieve an order with line items.</summary>
        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Calling GET api/orders/{OrderId}", id);
                var response = await _client.GetAsync($"api/orders/{id}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Order {OrderId} not found via API", id);
                    return null;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Order>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching order {OrderId} from API", id);
                throw;
            }
        }

        /// <summary>Calls GET api/orders/by-user/{userId} to retrieve orders for a specific user.</summary>
        public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId)
        {
            try
            {
                _logger.LogDebug("Calling GET api/orders/by-user/{UserId}", userId);
                var orders = await _client.GetFromJsonAsync<IEnumerable<Order>>($"api/orders/by-user/{userId}");
                return orders ?? Enumerable.Empty<Order>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching orders for user {UserId} from API", userId);
                throw;
            }
        }

        /// <summary>Calls POST api/orders to create a new order.</summary>
        public async Task<Order?> CreateOrderAsync(Order order)
        {
            try
            {
                _logger.LogInformation("Calling POST api/orders for user {UserId}", order?.UserId);
                var response = await _client.PostAsJsonAsync("api/orders", order);
                if (response.IsSuccessStatusCode)
                {
                    var created = await response.Content.ReadFromJsonAsync<Order>();
                    _logger.LogInformation("Created order {OrderId} via API", created?.Id);
                    return created;
                }

                _logger.LogWarning("Failed to create order via API. Status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating order via API");
                throw;
            }
        }

        /// <summary>Calls PUT api/orders/{id} to update order details.</summary>
        public async Task<Order?> UpdateOrderAsync(int id, Order order)
        {
            try
            {
                _logger.LogInformation("Calling PUT api/orders/{OrderId}", id);
                var response = await _client.PutAsJsonAsync($"api/orders/{id}", order);
                if (response.IsSuccessStatusCode)
                {
                    var updated = await response.Content.ReadFromJsonAsync<Order>();
                    _logger.LogInformation("Updated order {OrderId} via API", id);
                    return updated;
                }

                _logger.LogWarning("Failed to update order {OrderId} via API. Status: {StatusCode}", id, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating order {OrderId} via API", id);
                throw;
            }
        }

        /// <summary>Calls POST api/orders/{id}/place to finalize an order.</summary>
        public async Task<bool> PlaceOrderAsync(int id)
        {
            try
            {
                _logger.LogInformation("Calling POST api/orders/{OrderId}/place", id);
                var response = await _client.PostAsync($"api/orders/{id}/place", null);
                var success = response.IsSuccessStatusCode;
                if (!success)
                {
                    _logger.LogWarning("Failed to place order {OrderId} via API. Status: {StatusCode}", id, response.StatusCode);
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while placing order {OrderId} via API", id);
                throw;
            }
        }

        /// <summary>Calls DELETE api/orders/{id} to soft-delete an order.</summary>
        public async Task<bool> DeleteOrderAsync(int id)
        {
            try
            {
                _logger.LogInformation("Calling DELETE api/orders/{OrderId}", id);
                var response = await _client.DeleteAsync($"api/orders/{id}");
                var success = response.IsSuccessStatusCode;
                if (!success)
                {
                    _logger.LogWarning("Failed to delete order {OrderId} via API. Status: {StatusCode}", id, response.StatusCode);
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting order {OrderId} via API", id);
                throw;
            }
        }

        /// <summary>Calls GET api/orderitems/by-order/{orderId} to retrieve line items.</summary>
        public async Task<IEnumerable<OrderItem>> GetOrderLinesByOrderIdAsync(int orderId)
        {
            try
            {
                _logger.LogDebug("Calling GET api/orderitems/by-order/{OrderId}", orderId);
                var lines = await _client.GetFromJsonAsync<IEnumerable<OrderItem>>($"api/orderitems/by-order/{orderId}");
                return lines ?? Enumerable.Empty<OrderItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching lines for order {OrderId} from API", orderId);
                throw;
            }
        }

        /// <summary>Calls GET api/orderitems/{id} to retrieve a single line item.</summary>
        public async Task<OrderItem?> GetOrderLineByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Calling GET api/orderitems/{LineId}", id);
                var response = await _client.GetAsync($"api/orderitems/{id}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Order line {LineId} not found via API", id);
                    return null;
                }
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<OrderItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching line item {LineId} from API", id);
                throw;
            }
        }

        /// <summary>Calls POST api/orderitems to add a line item.</summary>
        public async Task<OrderItem?> CreateOrderLineAsync(OrderItem line)
        {
            try
            {
                _logger.LogInformation("Calling POST api/orderitems for order {OrderId}", line?.OrderId);
                var response = await _client.PostAsJsonAsync("api/orderitems", line);
                if (response.IsSuccessStatusCode)
                {
                    var created = await response.Content.ReadFromJsonAsync<OrderItem>();
                    _logger.LogInformation("Created order line {LineId} via API", created?.Id);
                    return created;
                }
                _logger.LogWarning("Failed to create order line via API. Status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating order line via API");
                throw;
            }
        }

        /// <summary>Calls PUT api/orderitems/{id} to update a line item.</summary>
        public async Task<bool> UpdateOrderLineAsync(int id, OrderItem line)
        {
            try
            {
                _logger.LogInformation("Calling PUT api/orderitems/{LineId}", id);
                var response = await _client.PutAsJsonAsync($"api/orderitems/{id}", line);
                var success = response.IsSuccessStatusCode;
                if (!success)
                {
                    _logger.LogWarning("Failed to update order line {LineId} via API. Status: {StatusCode}", id, response.StatusCode);
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating order line {LineId} via API", id);
                throw;
            }
        }

        /// <summary>Calls DELETE api/orderitems/{id} to soft-delete a line item.</summary>
        public async Task<bool> DeleteOrderLineAsync(int id)
        {
            try
            {
                _logger.LogInformation("Calling DELETE api/orderitems/{LineId}", id);
                var response = await _client.DeleteAsync($"api/orderitems/{id}");
                var success = response.IsSuccessStatusCode;
                if (!success)
                {
                    _logger.LogWarning("Failed to delete order line {LineId} via API. Status: {StatusCode}", id, response.StatusCode);
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting order line {LineId} via API", id);
                throw;
            }
        }
    }
}
