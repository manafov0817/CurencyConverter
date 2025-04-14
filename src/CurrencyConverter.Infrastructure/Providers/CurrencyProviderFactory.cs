using CurrencyConverter.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverter.Infrastructure.Providers
{
    public class CurrencyProviderFactory : ICurrencyProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, Type> _providerTypes;

        public CurrencyProviderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _providerTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                { "Frankfurter", typeof(FrankfurterApiProvider) }
            };
        }

        public ICurrencyProvider GetProvider(string providerName = null)
        {
            if (string.IsNullOrEmpty(providerName))
            {
                providerName = "Frankfurter";
            }

            if (!_providerTypes.TryGetValue(providerName, out var providerType))
            {
                throw new ArgumentException($"Provider '{providerName}' is not supported.");
            }

            return (ICurrencyProvider)_serviceProvider.GetRequiredService(providerType);
        }

        public IEnumerable<string> GetAvailableProviders()
        {
            return _providerTypes.Keys;
        }
    }
}
