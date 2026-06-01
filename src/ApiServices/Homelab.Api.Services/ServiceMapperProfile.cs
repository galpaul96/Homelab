using AutoMapper;
using Homelab.Domain.Api.Licensing;
using Homelab.Domain.Entities.Licensing;
using Homelab.Domain.Services.Licensing;

namespace Homelab.Api.Services
{
    internal class ServiceMapperProfile : Profile
    {
        public ServiceMapperProfile()
        {
            CreateMap<CreateClientRequest, CreateClientDto>();
            CreateMap<UpdateClientRequest, UpdateClientDto>()
                .ForMember(x => x.Id, x => x.Ignore());
            CreateMap<CreateProductRequest, CreateProductDto>();
            CreateMap<UpdateProductRequest, UpdateProductDto>()
                .ForMember(x => x.Id, x => x.Ignore());

            CreateMap<ClientDetailsDto, ClientResponse>();
            CreateMap<ProductClientDetailsDto, ProductClientResponse>();
            CreateMap<ProductDetailsDto, ProductResponse>();

            CreateMap<CreateClientDto, Client>()
                .ForMember(x => x.Id, x => x.Ignore())
                .ForMember(x => x.ExternalId, x => x.Ignore())
                .ForMember(x => x.CreatedDate, x => x.Ignore())
                .ForMember(x => x.UpdatedDate, x => x.Ignore())
                .ForMember(x => x.IsDeleted, x => x.Ignore())
                .ForMember(x => x.DeletedDate, x => x.Ignore())
                .ForMember(x => x.Products, x => x.Ignore());

            CreateMap<UpdateClientDto, Client>()
                .ForMember(x => x.Id, x => x.Ignore())
                .ForMember(x => x.ExternalId, x => x.Ignore())
                .ForMember(x => x.Name, x => x.Ignore())
                .ForMember(x => x.CreatedDate, x => x.Ignore())
                .ForMember(x => x.UpdatedDate, x => x.Ignore())
                .ForMember(x => x.IsDeleted, x => x.Ignore())
                .ForMember(x => x.DeletedDate, x => x.Ignore())
                .ForMember(x => x.Products, x => x.Ignore());

            CreateMap<Client, ClientDetailsDto>();
            CreateMap<Client, ProductClientDetailsDto>();

            CreateMap<CreateProductDto, Product>()
                .ForMember(x => x.Id, x => x.Ignore())
                .ForMember(x => x.ExternalId, x => x.Ignore())
                .ForMember(x => x.Client, x => x.Ignore())
                .ForMember(x => x.CreatedDate, x => x.Ignore())
                .ForMember(x => x.UpdatedDate, x => x.Ignore())
                .ForMember(x => x.IsDeleted, x => x.Ignore())
                .ForMember(x => x.DeletedDate, x => x.Ignore());

            CreateMap<UpdateProductDto, Product>()
                .ForMember(x => x.Id, x => x.Ignore())
                .ForMember(x => x.ExternalId, x => x.Ignore())
                .ForMember(x => x.ClientId, x => x.Ignore())
                .ForMember(x => x.Client, x => x.Ignore())
                .ForMember(x => x.CreatedDate, x => x.Ignore())
                .ForMember(x => x.UpdatedDate, x => x.Ignore())
                .ForMember(x => x.IsDeleted, x => x.Ignore())
                .ForMember(x => x.DeletedDate, x => x.Ignore());

            CreateMap<Product, ProductDetailsDto>();
        }
    }
}
