using Homelab.Web.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Homelab.Web.Services
{
    public static class Configurations
    {
        public static void ConfiugreServices(this IServiceCollection services)
        {
            services.ConfiugreGateway();

            services.TryAddScoped<IModulesService, ModulesService>();
        }
    }
}
