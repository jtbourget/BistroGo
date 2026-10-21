using System.Net.Http.Json;
using BistroGo.Core.Models;

namespace BistroGo.Web.Services
{
    /// <summary>
    /// Typed HTTP client for interacting with the Expenses API.
    /// </summary>
    public class ExpenseApiClient
    {
        private readonly HttpClient _client;

        public ExpenseApiClient(HttpClient client)
        {
            _client = client;
        }

        /// <summary>Calls GET api/expenses to retrieve operating expenses.</summary>
        public async Task<IEnumerable<Expense>> GetExpensesAsync()
        {
            var expenses = await _client.GetFromJsonAsync<IEnumerable<Expense>>("api/expenses");
            return expenses ?? Enumerable.Empty<Expense>();
        }

        /// <summary>Calls GET api/expenses/{id} to retrieve an expense by identifier.</summary>
        public async Task<Expense?> GetExpenseByIdAsync(int id)
        {
            var response = await _client.GetAsync($"api/expenses/{id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Expense>();
        }

        /// <summary>Calls GET api/expenses/by-category/{category} to retrieve expenses by category.</summary>
        public async Task<IEnumerable<Expense>> GetExpensesByCategoryAsync(string category)
        {
            var expenses = await _client.GetFromJsonAsync<IEnumerable<Expense>>($"api/expenses/by-category/{category}");
            return expenses ?? Enumerable.Empty<Expense>();
        }

        /// <summary>Calls POST api/expenses to record an operating expense.</summary>
        public async Task<Expense?> CreateExpenseAsync(Expense expense)
        {
            var response = await _client.PostAsJsonAsync("api/expenses", expense);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Expense>();
            }

            return null;
        }

        /// <summary>Calls PUT api/expenses/{id} to update an existing expense entry.</summary>
        public async Task<Expense?> UpdateExpenseAsync(int id, Expense expense)
        {
            var response = await _client.PutAsJsonAsync($"api/expenses/{id}", expense);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Expense>();
            }

            return null;
        }

        /// <summary>Calls DELETE api/expenses/{id} to soft-delete an expense entry.</summary>
        public async Task<bool> DeleteExpenseAsync(int id)
        {
            var response = await _client.DeleteAsync($"api/expenses/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}
