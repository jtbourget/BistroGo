namespace BistroGo.Web.Services
{
    public interface ITaxRateApiClient
    {
        Task<decimal> GetCurrentRateAsync();
        Task<TaxRateUpdateResponse> UpdateRateAsync(decimal rate);
    }
}
