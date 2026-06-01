using Homelab.Domain.Services.Licensing;

namespace Homelab.Api.Services;

public interface IProductService
{
    Task<IReadOnlyCollection<ProductDetailsDto>> GetProductsAsync(
        Guid? clientId = null,
        CancellationToken cancellationToken = default);

    Task<ProductDetailsDto?> GetProductAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ProductDetailsDto> CreateProductAsync(
        CreateProductDto product,
        CancellationToken cancellationToken = default);

    Task<ProductDetailsDto?> UpdateProductAsync(
        UpdateProductDto product,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteProductAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
