/**
 * Tax Rate Management
 * Fetches and caches the current tax rate from the server.
 * The server maintains the authoritative rate; this script provides client-side caching.
 */

const TaxRateManager = (() => {
    const CACHE_KEY = 'bistroGo_taxRate';
    const CACHE_TIMESTAMP_KEY = 'bistroGo_taxRateTimestamp';
    const CACHE_DURATION_MS = 5 * 60 * 1000; // Cache for 5 minutes
    const API_ENDPOINT = '/api/taxrates/current';

    /**
     * Fetches the current tax rate from the server and caches it in localStorage.
     * Falls back to cached value if network request fails.
     * @returns {Promise<number>} The tax rate as a decimal (e.g., 0.08 for 8%)
     */
    async function fetchTaxRate() {
        try {
            const response = await fetch(API_ENDPOINT, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include' // Include cookies for authentication
            });

            if (!response.ok) {
                console.warn(`Failed to fetch tax rate: ${response.status}. Using cached value.`);
                return getCachedTaxRate();
            }

            const data = await response.json();
            const rate = parseFloat(data.rate);

            if (isNaN(rate)) {
                console.error('Invalid tax rate from server:', data.rate);
                return getCachedTaxRate();
            }

            // Cache the rate and timestamp
            localStorage.setItem(CACHE_KEY, rate.toString());
            localStorage.setItem(CACHE_TIMESTAMP_KEY, Date.now().toString());

            console.log(`Tax rate updated to ${(rate * 100).toFixed(1)}%`);
            return rate;
        } catch (error) {
            console.error('Error fetching tax rate:', error);
            return getCachedTaxRate();
        }
    }

    /**
     * Retrieves the cached tax rate from localStorage.
     * Returns default 0.08 (8%) if no cache is available.
     * @returns {number} The cached tax rate or default 0.08
     */
    function getCachedTaxRate() {
        const cached = localStorage.getItem(CACHE_KEY);
        if (cached) {
            const rate = parseFloat(cached);
            if (!isNaN(rate)) {
                console.log(`Using cached tax rate: ${(rate * 100).toFixed(1)}%`);
                return rate;
            }
        }
        console.log('No cached tax rate. Using default 0.08 (8%)');
        return 0.08;
    }

    /**
     * Checks if the cached tax rate is still valid within the cache duration.
     * @returns {boolean} True if cache is still valid, false otherwise
     */
    function isCacheValid() {
        const timestamp = localStorage.getItem(CACHE_TIMESTAMP_KEY);
        if (!timestamp) return false;

        const cacheAge = Date.now() - parseInt(timestamp);
        return cacheAge < CACHE_DURATION_MS;
    }

    /**
     * Gets the current tax rate, using cache if valid, otherwise fetches from server.
     * @returns {Promise<number>} The tax rate as a decimal
     */
    async function getOrFetchTaxRate() {
        if (isCacheValid()) {
            return getCachedTaxRate();
        }
        return await fetchTaxRate();
    }

    /**
     * Clears the cached tax rate (useful for testing or manual refresh).
     */
    function clearCache() {
        localStorage.removeItem(CACHE_KEY);
        localStorage.removeItem(CACHE_TIMESTAMP_KEY);
        console.log('Tax rate cache cleared');
    }

    /**
     * Initializes the tax rate manager on page load.
     * Fetches and caches the current rate.
     */
    async function init() {
        console.log('Initializing TaxRateManager...');
        await fetchTaxRate();
    }

    // Public API
    return {
        fetchTaxRate,
        getCachedTaxRate,
        isCacheValid,
        getOrFetchTaxRate,
        clearCache,
        init
    };
})();

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    TaxRateManager.init();
});
