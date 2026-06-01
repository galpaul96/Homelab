using Homelab.Api.Controllers;
using Homelab.Api.Services;
using Homelab.Domain.Api.Licensing;
using Homelab.Domain.Services.Licensing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Homelab.Tests;

public class ProductsControllerTests
{
    [Fact]
    public async Task GetProductsAsyncPassesClientFilter()
    {
        var clientId = Guid.NewGuid();
        var service = new FakeProductService
        {
            GetProducts = requestedClientId =>
            {
                Assert.Equal(clientId, requestedClientId);
                return [];
            }
        };
        var controller = CreateController(service);

        var result = await controller.GetProductsAsync(clientId, TestContext.Current.CancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyCollection<ProductResponse>>(ok.Value));
    }

    [Fact]
    public async Task GetProductAsyncReturnsNotFoundWhenProductDoesNotExist()
    {
        var controller = CreateController(new FakeProductService());

        var result = await controller.GetProductAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateProductAsyncReturnsCreatedProductWithClientSummary()
    {
        var clientId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var service = new FakeProductService
        {
            CreateProduct = request => BuildProduct(productId, clientId, request.Name)
        };
        var controller = CreateController(service);

        var result = await controller.CreateProductAsync(
            new CreateProductRequest
            {
                ClientId = clientId,
                Name = "Portal",
                Description = "Client portal",
                Type = "Web",
                HostedOn = "Azure"
            },
            TestContext.Current.CancellationToken);

        var created = Assert.IsType<CreatedAtRouteResult>(result.Result);
        Assert.Equal("GetProduct", created.RouteName);

        var response = Assert.IsType<ProductResponse>(created.Value);
        Assert.Equal(productId, response.Id);
        Assert.Equal(clientId, response.ClientId);
        Assert.Equal("Portal", response.Name);
        Assert.NotNull(response.Client);
        Assert.Equal("Acme", response.Client.Name);
    }

    [Fact]
    public async Task CreateProductAsyncReturnsBadRequestWhenClientDoesNotExist()
    {
        var service = new FakeProductService
        {
            CreateProduct = _ => throw new InvalidOperationException("Client was not found.")
        };
        var controller = CreateController(service);

        var result = await controller.CreateProductAsync(
            new CreateProductRequest
            {
                ClientId = Guid.NewGuid(),
                Name = "Portal"
            },
            TestContext.Current.CancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Product could not be created.", problem.Title);
    }

    [Fact]
    public async Task UpdateProductAsyncReturnsUpdatedProduct()
    {
        var productId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var service = new FakeProductService
        {
            UpdateProduct = request => BuildProduct(productId, clientId, request.Name, request.Description, request.Type, request.HostedOn)
        };
        var controller = CreateController(service);

        var result = await controller.UpdateProductAsync(
            productId,
            new UpdateProductRequest
            {
                Name = "Updated",
                Description = "Updated description",
                Type = "Api",
                HostedOn = "Homelab"
            },
            TestContext.Current.CancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ProductResponse>(ok.Value);
        Assert.Equal("Updated", response.Name);
        Assert.Equal("Updated description", response.Description);
        Assert.Equal("Api", response.Type);
        Assert.Equal("Homelab", response.HostedOn);
    }

    [Fact]
    public async Task DeleteProductAsyncReturnsNoContentWhenProductIsDeleted()
    {
        var service = new FakeProductService
        {
            DeleteProduct = _ => true
        };
        var controller = CreateController(service);

        var result = await controller.DeleteProductAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.IsType<NoContentResult>(result);
    }

    private static ProductsController CreateController(FakeProductService service)
    {
        return new ProductsController(
            service,
            NullLogger<ProductsController>.Instance);
    }

    private static ProductDetailsDto BuildProduct(
        Guid productId,
        Guid clientId,
        string name,
        string? description = null,
        string? type = null,
        string? hostedOn = null)
    {
        return new ProductDetailsDto
        {
            Id = productId,
            ExternalId = Guid.NewGuid(),
            ClientId = clientId,
            Name = name,
            Description = description,
            Type = type,
            HostedOn = hostedOn,
            Client = new ProductClientDetailsDto
            {
                Id = clientId,
                ExternalId = Guid.NewGuid(),
                Name = "Acme"
            }
        };
    }

    private sealed class FakeProductService : IProductService
    {
        public Func<Guid?, IReadOnlyCollection<ProductDetailsDto>>? GetProducts { get; init; }

        public Func<CreateProductDto, ProductDetailsDto>? CreateProduct { get; init; }

        public Func<UpdateProductDto, ProductDetailsDto?>? UpdateProduct { get; init; }

        public Func<Guid, bool>? DeleteProduct { get; init; }

        public Task<IReadOnlyCollection<ProductDetailsDto>> GetProductsAsync(
            Guid? clientId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetProducts?.Invoke(clientId) ?? []);
        }

        public Task<ProductDetailsDto?> GetProductAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ProductDetailsDto?>(null);
        }

        public Task<ProductDetailsDto> CreateProductAsync(
            CreateProductDto product,
            CancellationToken cancellationToken = default)
        {
            if (CreateProduct is null)
            {
                throw new ArgumentException("Product name is required.", nameof(product));
            }

            return Task.FromResult(CreateProduct(product));
        }

        public Task<ProductDetailsDto?> UpdateProductAsync(
            UpdateProductDto product,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UpdateProduct?.Invoke(product));
        }

        public Task<bool> DeleteProductAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeleteProduct?.Invoke(id) ?? false);
        }
    }
}
