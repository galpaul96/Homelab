using Homelab.Domain.Web.Licensing;

namespace Homelab.Web.Services
{
    public interface IProductService
    {
        Task<IReadOnlyCollection<WebProductModel>> GetProductsAsync(Guid? clientId = null);

        Task<WebProductModel?> GetProductAsync(Guid id);

        Task<WebProductModel> CreateProductAsync(WebProductDraft product);

        Task<WebProductModel?> UpdateProductAsync(Guid id, WebProductEditModel product);

        Task<bool> DeleteProductAsync(Guid id);
    }
}
