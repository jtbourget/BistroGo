using System.Net.Http.Json;
using BistroGo.Core.Models;

namespace BistroGo.Web.Services
{
    /// <summary>
    /// Typed HTTP client for interacting with the Orders API.
    /// </summary>
    public class OrderApiClient
    {
        private readonly HttpClient _client;

        public OrderApiClient(HttpClient client)
        {
            _client = client;
        }

        /// <summary>Calls GET api/orders to retrieve all orders.</summary>
        public async Task<IEnumerable<Order>> GetOrdersAsync()
        {
            var orders = await _client.GetFromJsonAsync<IEnumerable<Order>>("api/orders");
            return orders ?? Enumerable.Empty<Order>();
        }

        /// <summary>Calls GET api/orders/{id} to retrieve an order with line items.</summary>
        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            var response = await _client.GetAsync($"api/orders/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Order>();
        }

        /// <summary>Calls GET api/orders/by-user/{userId} to retrieve orders for a specific user.</summary>
        public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId)
        {
            var orders = await _client.GetFromJsonAsync<IEnumerable<Order>>($"api/orders/by-user/{userId}");
            return orders ?? Enumerable.Empty<Order>();
        }

        /// <summary>Calls POST api/orders to create a new order.</summary>
        public async Task<Order?> CreateOrderAsync(Order order)
        {
            var response = await _client.PostAsJsonAsync("api/orders", order);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Order>();
            }

            return null;
        }

        /// <summary>Calls PUT api/orders/{id} to update order details.</summary>
        public async Task<Order?> UpdateOrderAsync(int id, Order order)
        {
            var response = await _client.PutAsJsonAsync($"api/orders/{id}", order);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Order>();
            }

            return null;
        }

        /// <summary>Calls POST api/orders/{id}/place to finalize an order.</summary>
        public async Task<bool> PlaceOrderAsync(int id)
        {
            var response = await _client.PostAsync($"api/orders/{id}/place", null);
            return response.IsSuccessStatusCode;
        }

        /// <summary>Calls DELETE api/orders/{id} to soft-delete an order.</summary>
        public async Task<bool> DeleteOrderAsync(int id)
        {
            var response = await _client.DeleteAsync($"api/orders/{id}");
            return response.IsSuccessStatusCode;
        }

        /// <summary>Calls GET api/orderitems/by-order/{orderId} to retrieve line items.</summary>
        public async Task<IEnumerable<OrderItem>> GetOrderLinesByOrderIdAsync(int orderId)
        {
            var lines = await _client.GetFromJsonAsync<IEnumerable<OrderItem>>($"api/orderitems/by-order/{orderId}");
            return lines ?? Enumerable.Empty<OrderItem>();
        }

        /// <summary>Calls GET api/orderitems/{id} to retrieve a single line item.</summary>
        public async Task<OrderItem?> GetOrderLineByIdAsync(int id)
        {
            var response = await _client.GetAsync($"api/orderitems/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<OrderItem>();
        }

        /// <summary>Calls POST api/orderitems to add a line item.</summary>
        public async Task<OrderItem?> CreateOrderLineAsync(OrderItem line)
        {
            var response = await _client.PostAsJsonAsync("api/orderitems", line);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<OrderItem>();
            }
            return null;
        }

        /// <summary>Calls PUT api/orderitems/{id} to update a line item.</summary>
        public async Task<bool> UpdateOrderLineAsync(int id, OrderItem line)
        {
            var response = await _client.PutAsJsonAsync($"api/orderitems/{id}", line);
            return response.IsSuccessStatusCode;
        }

        /// <summary>Calls DELETE api/orderitems/{id} to soft-delete a line item.</summary>
        public async Task<bool> DeleteOrderLineAsync(int id)
        {
            var response = await _client.DeleteAsync($"api/orderitems/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
