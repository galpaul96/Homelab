using AutoMapper;
using Homelab.Api.Ef;
using Homelab.Domain.Entities.Licensing;
using Homelab.Domain.Services.Licensing;
using Microsoft.EntityFrameworkCore;

namespace Homelab.Api.Services;

internal class ProductService : IProductService
{
    private readonly IRepository _repository;
    private readonly IMapper _mapper;

    public ProductService(
        IRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<ProductDetailsDto>> GetProductsAsync(
        Guid? clientId = null,
        CancellationToken cancellationToken = default)
    {
        var query = GetProductDetailsQuery().AsNoTracking();

        if (clientId.HasValue)
        {
            query = query.Where(x => x.ClientId == clientId.Value);
        }

        var products = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.CreatedDate)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyCollection<ProductDetailsDto>>(products);
    }

    public async Task<ProductDetailsDto?> GetProductAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = await GetProductDetailsQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return product is null
            ? null
            : _mapper.Map<ProductDetailsDto>(product);
    }

    public async Task<ProductDetailsDto> CreateProductAsync(
        CreateProductDto request,
        CancellationToken cancellationToken = default)
    {
        var name = TrimToNull(request.Name);
        if (name is null)
        {
            throw new ArgumentException("Product name is required.", nameof(request));
        }

        var client = await _repository.GetAllAsync<Client>()
            .FirstOrDefaultAsync(x => x.Id == request.ClientId, cancellationToken)
            ?? throw new InvalidOperationException("Client was not found.");

        var now = DateTime.UtcNow;
        var product = _mapper.Map<Product>(request);
        product.Client = client;
        product.ClientId = client.Id;
        product.Name = name;
        product.Description = TrimToNull(request.Description);
        product.Type = TrimToNull(request.Type);
        product.HostedOn = TrimToNull(request.HostedOn);
        product.CreatedDate = now;
        product.UpdatedDate = now;

        var createdProduct = await _repository.AddAsync(product);

        return _mapper.Map<ProductDetailsDto>(createdProduct);
    }

    public async Task<ProductDetailsDto?> UpdateProductAsync(
        UpdateProductDto request,
        CancellationToken cancellationToken = default)
    {
        var name = TrimToNull(request.Name);
        if (name is null)
        {
            throw new ArgumentException("Product name is required.", nameof(request));
        }

        var product = await GetProductDetailsQuery()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (product is null)
        {
            return null;
        }

        product.Name = name;
        product.Description = TrimToNull(request.Description);
        product.Type = TrimToNull(request.Type);
        product.HostedOn = TrimToNull(request.HostedOn);

        await _repository.UpdateAsync(product);

        return _mapper.Map<ProductDetailsDto>(product);
    }

    public async Task<bool> DeleteProductAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var exists = await _repository.GetAllAsync<Product>()
            .AnyAsync(x => x.Id == id, cancellationToken);

        if (!exists)
        {
            return false;
        }

        await _repository.DeleteAsync<Product>(id);
        return true;
    }

    private IQueryable<Product> GetProductDetailsQuery()
    {
        return _repository.GetAllAsync<Product>()
            .Include(x => x.Client);
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
