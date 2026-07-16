using Homelab.Web.Gateway.ExternalApis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Homelab.Web.Gateway
{
    public static class Configurations
    {
        public static void ConfigureGateway(this IServiceCollection services, IConfiguration configuration)
        {
            services.TryAddScoped<IGatewayClient, GatewayClient>();

            var baseAddress = configuration.GetSection(ApiClientOptions.SectionName)[nameof(ApiClientOptions.BaseAddress)]
                ?? throw new InvalidOperationException("Configuration key 'ApiClient:BaseAddress' is required.");

            if (!Uri.TryCreate(baseAddress, UriKind.Absolute, out var apiBaseAddress))
            {
                throw new InvalidOperationException("Configuration key 'ApiClient:BaseAddress' must be an absolute URI.");
            }

            services.AddHttpClient("api", client => client.BaseAddress = apiBaseAddress)
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        }
    }
}
