using AutoMapper;
using Homelab.Api.Ef;
using Homelab.Api.MongoDb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Homelab.Api.Services
{
    public static class Configurations
    {
        public static void ConfigureServices(this IServiceCollection services,
            IConfiguration configuration)
        {
            services.ConfigureRepository(configuration);
            services.ConfigureMongoDb(configuration);
            services.TryAddScoped<IClientService, ClientService>();
            services.TryAddScoped<IProductService, ProductService>();

            services.AddAutoMapper(x =>
            {
                x.AddProfile(new ServiceMapperProfile());
            });

        }
    }
}
