namespace CurrencyConverter.Core.Interfaces
{
    public interface ICurrencyProviderFactory
    {
        ICurrencyProvider GetProvider(string providerName = null);
        IEnumerable<string> GetAvailableProviders();
    }
}
