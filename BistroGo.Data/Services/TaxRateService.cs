using BistroGo.Core.Interfaces;
using BistroGo.Core.Pricing;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;

namespace BistroGo.Data.Services
{
    /// <summary>
    /// In-memory implementation of ITaxRateService with file-based persistence.
    /// The service maintains the current tax rate in memory and manages updates.
    /// Validation ensures rates stay within permitted bounds (0% to 20%).
    /// Tax rate is persisted to a JSON file so updates survive application restarts.
    /// </summary>
    public class TaxRateService : ITaxRateService
    {
        private decimal _currentRate;
        private readonly object _lockObject = new();
        private readonly string _taxRateFilePath;

        /// <summary>
        /// Minimum allowed tax rate (0%).
        /// </summary>
        private const decimal MinRate = 0.00m;

        /// <summary>
        /// Maximum allowed tax rate (20%).
        /// </summary>
        private const decimal MaxRate = 0.20m;

        /// <summary>
        /// Filename for tax rate persistence.
        /// </summary>
        private const string TaxRateFileName = "taxrate.json";

        public TaxRateService(IWebHostEnvironment env)
        {
            // Determine the file path for persistence
            var appDataPath = Path.Combine(env.ContentRootPath, "App_Data");
            _taxRateFilePath = Path.Combine(appDataPath, TaxRateFileName);

            // Create App_Data directory if it doesn't exist
            try
            {
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Unable to create App_Data directory: {ex.Message}");
            }

            // Load the rate from file if it exists, otherwise use the default
            _currentRate = LoadRateFromFile() ?? PricingConstants.TaxRate;
        }

        /// <summary>
        /// Retrieves the current active tax rate in a thread-safe manner.
        /// </summary>
        public decimal GetCurrentRate()
        {
            lock (_lockObject)
            {
                return _currentRate;
            }
        }

        /// <summary>
        /// Updates the current active tax rate if validation passes.
        /// Thread-safe for concurrent updates (last-one-wins).
        /// Persists the rate to file after successful update.
        /// </summary>
        /// <param name="rate">The new rate as a decimal (0.00 to 0.20)</param>
        /// <returns>True if update succeeded; false if validation failed.</returns>
        public bool UpdateRate(decimal rate)
        {
            // Validate the rate
            if (!IsValidRate(rate))
            {
                return false;
            }

            lock (_lockObject)
            {
                _currentRate = rate;
            }

            // Persist to file
            try
            {
                SaveRateToFile(rate);
            }
            catch (Exception ex)
            {
                // Log error but don't fail the update
                Console.WriteLine($"Warning: Failed to persist tax rate to file: {ex.Message}");
            }

            return true;
        }

        /// <summary>
        /// Validates that the rate is within permitted bounds.
        /// </summary>
        private static bool IsValidRate(decimal rate)
        {
            return rate >= MinRate && rate <= MaxRate;
        }

        /// <summary>
        /// Loads the tax rate from the JSON file.
        /// Returns null if file doesn't exist or is invalid.
        /// </summary>
        private decimal? LoadRateFromFile()
        {
            try
            {
                if (!File.Exists(_taxRateFilePath))
                {
                    return null;
                }

                var json = File.ReadAllText(_taxRateFilePath);
                var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("rate", out var rateElement) &&
                    rateElement.TryGetDecimal(out var rate))
                {
                    Console.WriteLine($"Loaded tax rate from file: {rate}");
                    return rate;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading tax rate file: {ex.Message}. Using default rate.");
                return null;
            }
        }

        /// <summary>
        /// Saves the tax rate to the JSON file.
        /// </summary>
        private void SaveRateToFile(decimal rate)
        {
            var dir = Path.GetDirectoryName(_taxRateFilePath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var data = new { rate };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_taxRateFilePath, json);
            Console.WriteLine($"Saved tax rate to file: {rate}");
        }
    }
}