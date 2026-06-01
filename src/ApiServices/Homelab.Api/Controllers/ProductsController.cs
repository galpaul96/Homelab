using Homelab.Api.Services;
using Homelab.Domain.Api.Licensing;
using Homelab.Domain.Services.Licensing;
using Microsoft.AspNetCore.Mvc;

namespace Homelab.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private const string GetProductRoute = "GetProduct";

    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductService productService,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    [HttpGet(Name = "GetProducts")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ProductResponse>>> GetProductsAsync(
        [FromQuery] Guid? clientId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting products for client {ClientId}.", clientId);

        var products = await _productService.GetProductsAsync(clientId, cancellationToken);

        return Ok(products.Select(MapProduct).ToArray());
    }

    [HttpGet("{id:guid}", Name = GetProductRoute)]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetProductAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting product {ProductId}.", id);

        var product = await _productService.GetProductAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        return Ok(MapProduct(product));
    }

    [HttpPost(Name = "CreateProduct")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductResponse>> CreateProductAsync(
        CreateProductRequest product,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating product {ProductName} for client {ClientId}.", product.Name, product.ClientId);

        try
        {
            var createdProduct = await _productService.CreateProductAsync(
                new CreateProductDto
                {
                    ClientId = product.ClientId,
                    Name = product.Name,
                    Description = product.Description,
                    Type = product.Type,
                    HostedOn = product.HostedOn
                },
                cancellationToken);
            var response = MapProduct(createdProduct);

            return CreatedAtRoute(
                GetProductRoute,
                new { id = response.Id },
                response);
        }
        catch (ArgumentException ex)
        {
            return BadRequestProblem("Invalid product.", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestProblem("Product could not be created.", ex.Message);
        }
    }

    [HttpPut("{id:guid}", Name = "UpdateProduct")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> UpdateProductAsync(
        Guid id,
        UpdateProductRequest product,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating product {ProductId}.", id);

        try
        {
            var updatedProduct = await _productService.UpdateProductAsync(
                new UpdateProductDto
                {
                    Id = id,
                    Name = product.Name,
                    Description = product.Description,
                    Type = product.Type,
                    HostedOn = product.HostedOn
                },
                cancellationToken);
            if (updatedProduct is null)
            {
                return NotFound();
            }

            return Ok(MapProduct(updatedProduct));
        }
        catch (ArgumentException ex)
        {
            return BadRequestProblem("Invalid product.", ex.Message);
        }
    }

    [HttpDelete("{id:guid}", Name = "DeleteProduct")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProductAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting product {ProductId}.", id);

        var deleted = await _productService.DeleteProductAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    private BadRequestObjectResult BadRequestProblem(
        string title,
        string detail)
    {
        return BadRequest(new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = StatusCodes.Status400BadRequest
        });
    }

    private static ProductResponse MapProduct(ProductDetailsDto product)
    {
        return new ProductResponse
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

    private static ProductClientResponse MapClient(ProductClientDetailsDto client)
    {
        return new ProductClientResponse
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
