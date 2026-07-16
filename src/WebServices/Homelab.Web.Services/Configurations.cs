using Homelab.Web.Gateway;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Homelab.Web.Services
{
    public static class Configurations
    {
        public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.ConfigureGateway(configuration);

            services.TryAddScoped<IClientService, ClientService>();
            services.TryAddScoped<IProductService, ProductService>();
        }
    }
}
