using System.Net.Http.Json;
using BistroGo.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BistroGo.Web.Services
{
    /// <summary>
    /// Typed HTTP client for interacting with the Expenses API.
    /// </summary>
    public class ExpenseApiClient
    {
        private readonly HttpClient _client;
        private readonly ILogger<ExpenseApiClient> _logger;

        public ExpenseApiClient(HttpClient client, ILogger<ExpenseApiClient>? logger = null)
        {
            _client = client;
            _logger = logger ?? NullLogger<ExpenseApiClient>.Instance;
        }

        /// <summary>Calls GET api/expenses to retrieve operating expenses.</summary>
        public async Task<IEnumerable<Expense>> GetExpensesAsync()
        {
            try
            {
                _logger.LogDebug("Calling GET api/expenses");
                var expenses = await _client.GetFromJsonAsync<IEnumerable<Expense>>("api/expenses");
                return expenses ?? Enumerable.Empty<Expense>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching expenses from API");
                throw;
            }
        }

        /// <summary>Calls GET api/expenses/{id} to retrieve an expense by identifier.</summary>
        public async Task<Expense?> GetExpenseByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Calling GET api/expenses/{ExpenseId}", id);
                var response = await _client.GetAsync($"api/expenses/{id}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Expense {ExpenseId} not found via API", id);
                    return null;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Expense>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching expense {ExpenseId} from API", id);
                throw;
            }
        }

        /// <summary>Calls GET api/expenses/by-category/{category} to retrieve expenses by category.</summary>
        public async Task<IEnumerable<Expense>> GetExpensesByCategoryAsync(string category)
        {
            try
            {
                _logger.LogDebug("Calling GET api/expenses/by-category/{Category}", category);
                var expenses = await _client.GetFromJsonAsync<IEnumerable<Expense>>($"api/expenses/by-category/{category}");
                return expenses ?? Enumerable.Empty<Expense>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching expenses by category {Category} from API", category);
                throw;
            }
        }

        /// <summary>Calls POST api/expenses to record an operating expense.</summary>
        public async Task<Expense?> CreateExpenseAsync(Expense expense)
        {
            try
            {
                _logger.LogInformation("Calling POST api/expenses for {Description}", expense?.Description);
                var response = await _client.PostAsJsonAsync("api/expenses", expense);
                if (response.IsSuccessStatusCode)
                {
                    var created = await response.Content.ReadFromJsonAsync<Expense>();
                    _logger.LogInformation("Created expense {ExpenseId} via API", created?.Id);
                    return created;
                }

                _logger.LogWarning("Failed to create expense via API. Status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating expense via API");
                throw;
            }
        }

        /// <summary>Calls PUT api/expenses/{id} to update an existing expense entry.</summary>
        public async Task<Expense?> UpdateExpenseAsync(int id, Expense expense)
        {
            try
            {
                _logger.LogInformation("Calling PUT api/expenses/{ExpenseId}", id);
                var response = await _client.PutAsJsonAsync($"api/expenses/{id}", expense);
                if (response.IsSuccessStatusCode)
                {
                    var updated = await response.Content.ReadFromJsonAsync<Expense>();
                    _logger.LogInformation("Updated expense {ExpenseId} via API", id);
                    return updated;
                }

                _logger.LogWarning("Failed to update expense {ExpenseId} via API. Status: {StatusCode}", id, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating expense {ExpenseId} via API", id);
                throw;
            }
        }

        /// <summary>Calls DELETE api/expenses/{id} to soft-delete an expense entry.</summary>
        public async Task<bool> DeleteExpenseAsync(int id)
        {
            try
            {
                _logger.LogInformation("Calling DELETE api/expenses/{ExpenseId}", id);
                var response = await _client.DeleteAsync($"api/expenses/{id}");
                var success = response.IsSuccessStatusCode;
                if (!success)
                {
                    _logger.LogWarning("Failed to delete expense {ExpenseId} via API. Status: {StatusCode}", id, response.StatusCode);
                }
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting expense {ExpenseId} via API", id);
                throw;
            }
        }
    }
}
