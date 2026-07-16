using Homelab.Domain.Api.Licensing;
using Homelab.Domain.Web.Licensing;
using Homelab.Web.Gateway.ExternalApis;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;

namespace Homelab.Web.Services
{
    internal class ProductService : IProductService
    {
        private readonly ILogger<ProductService> logger;
        private readonly IGatewayClient gatewayClient;

        public ProductService(
            ILogger<ProductService> logger,
            IGatewayClient gatewayClient)
        {
            this.logger = logger;
            this.gatewayClient = gatewayClient;
        }

        public async Task<IReadOnlyCollection<WebProductModel>> GetProductsAsync(Guid? clientId = null)
        {
            logger.LogInformation("Getting products for client {ClientId}.", clientId);

            var route = clientId.HasValue
                ? $"Products?clientId={clientId.Value}"
                : "Products";
            var response = await gatewayClient.GetAsync(route);
            var products = await ReadResponseAsync<IReadOnlyCollection<ProductResponse>>(
                response,
                "products") ?? [];

            return products.Select(MapProduct).ToArray();
        }

        public async Task<WebProductModel?> GetProductAsync(Guid id)
        {
            logger.LogInformation("Getting product {ProductId}.", id);

            var response = await gatewayClient.GetAsync($"Products/{id}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation("Product {ProductId} was not found.", id);
                return null;
            }

            var product = await ReadResponseAsync<ProductResponse>(response, "product");
            return product is null ? null : MapProduct(product);
        }

        public async Task<WebProductModel> CreateProductAsync(WebProductDraft product)
        {
            logger.LogInformation("Creating product {ProductName} for client {ClientId}.", product.Name, product.ClientId);

            var response = await gatewayClient.PostAsync(
                "Products",
                new CreateProductRequest
                {
                    ClientId = product.ClientId,
                    Name = product.Name,
                    Description = product.Description,
                    Type = product.Type,
                    HostedOn = product.HostedOn
                });

            var createdProduct = await ReadResponseAsync<ProductResponse>(response, "created product")
                ?? throw new InvalidOperationException("The products API returned an empty product response.");

            return MapProduct(createdProduct);
        }

        public async Task<WebProductModel?> UpdateProductAsync(Guid id, WebProductEditModel product)
        {
            logger.LogInformation("Updating product {ProductId}.", id);

            var response = await gatewayClient.PutAsync(
                $"Products/{id}",
                new UpdateProductRequest
                {
                    Name = product.Name,
                    Description = product.Description,
                    Type = product.Type,
                    HostedOn = product.HostedOn
                });

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation("Product {ProductId} was not found for update.", id);
                return null;
            }

            var updatedProduct = await ReadResponseAsync<ProductResponse>(response, "updated product");
            return updatedProduct is null ? null : MapProduct(updatedProduct);
        }

        public async Task<bool> DeleteProductAsync(Guid id)
        {
            logger.LogInformation("Deleting product {ProductId}.", id);

            var response = await gatewayClient.DeleteAsync($"Products/{id}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation("Product {ProductId} was not found for deletion.", id);
                return false;
            }

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var body = await response.Content.ReadAsStringAsync();
            logger.LogError(
                "Failed to delete product. Status code: {StatusCode}. Response: {ResponseBody}",
                response.StatusCode,
                body);

            throw new Exception($"Failed to delete product. Status code: {response.StatusCode}");
        }

        private async Task<T?> ReadResponseAsync<T>(HttpResponseMessage response, string resourceName)
        {
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<T>();
                logger.LogInformation("Successfully retrieved {ResourceName}.", resourceName);
                return result;
            }

            var body = await response.Content.ReadAsStringAsync();
            logger.LogError(
                "Failed to retrieve {ResourceName}. Status code: {StatusCode}. Response: {ResponseBody}",
                resourceName,
                response.StatusCode,
                body);

            throw new Exception($"Failed to retrieve {resourceName}. Status code: {response.StatusCode}");
        }

        private static WebProductModel MapProduct(ProductResponse product)
        {
            return new WebProductModel
            {
                Id = product.Id,
                ExternalId = product.ExternalId,
                ClientId = product.ClientId,
                Client = product.Client is null ? null : MapClient(product.Client),
                Name = product.Name,
                Description = product.Description,
                Type = product.Type,
                HostedOn = product.HostedOn,
                CreatedDate = product.CreatedDate,
                UpdatedDate = product.UpdatedDate
            };
        }

        private static WebProductClientModel MapClient(ProductClientResponse client)
        {
            return new WebProductClientModel
            {
                Id = client.Id,
                ExternalId = client.ExternalId,
                Name = client.Name,
                Description = client.Description,
                Notes = client.Notes,
                CreatedDate = client.CreatedDate,
                UpdatedDate = client.UpdatedDate
            };
        }
    }
}
