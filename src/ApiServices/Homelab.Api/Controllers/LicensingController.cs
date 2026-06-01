using Homelab.Api.Services;
using Homelab.Domain.Api.Licensing;
using Homelab.Domain.Services.Licensing;
using Microsoft.AspNetCore.Mvc;

namespace Homelab.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class LicensingController : ControllerBase
{
    private const string GetClientRoute = "GetLicensingClient";

    private readonly IClientService _clientService;
    private readonly ILogger<LicensingController> _logger;

    public LicensingController(
        IClientService clientService,
        ILogger<LicensingController> logger)
    {
        _clientService = clientService;
        _logger = logger;
    }

    [HttpGet("clients", Name = "GetLicensingClients")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ClientResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ClientResponse>>> GetClientsAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting clients.");

        var clients = await _clientService.GetClientsAsync(cancellationToken);

        return Ok(clients.Select(MapClient).ToArray());
    }

    [HttpGet("clients/{id:guid}", Name = GetClientRoute)]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientResponse>> GetClientAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting client {ClientId}.", id);

        var client = await _clientService.GetClientAsync(id, cancellationToken);

        if (client is null)
        {
            return NotFound();
        }

        return Ok(MapClient(client));
    }

    [HttpPost("clients", Name = "CreateLicensingClient")]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClientResponse>> CreateClientAsync(
        CreateClientRequest client,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating client {ClientName}.", client.Name);

        try
        {
            var createdClient = await _clientService.CreateClientAsync(
                new CreateClientDto
                {
                    Name = client.Name,
                    Description = client.Description,
                    Notes = client.Notes
                },
                cancellationToken);
            var response = MapClient(createdClient);

            return CreatedAtRoute(
                GetClientRoute,
                new { id = response.Id },
                response);
        }
        catch (ArgumentException ex)
        {
            return BadRequestProblem("Invalid client.", ex.Message);
        }
    }

    [HttpPut("clients/{id:guid}", Name = "UpdateLicensingClient")]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientResponse>> UpdateClientAsync(
        Guid id,
        UpdateClientRequest client,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating client {ClientId}.", id);

        var updatedClient = await _clientService.UpdateClientAsync(
            new UpdateClientDto
            {
                Id = id,
                Description = client.Description,
                Notes = client.Notes
            },
            cancellationToken);
        if (updatedClient is null)
        {
            return NotFound();
        }

        return Ok(MapClient(updatedClient));
    }

    [HttpDelete("clients/{id:guid}", Name = "DeleteLicensingClient")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteClientAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting client {ClientId}.", id);

        var deleted = await _clientService.DeleteClientAsync(id, cancellationToken);
        if (deleted.NotFound)
        {
            return NotFound();
        }

        if (deleted.BlockedByProducts)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Client could not be deleted.",
                Detail = deleted.Message,
                Status = StatusCodes.Status409Conflict
            });
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

    private static ClientResponse MapClient(ClientDetailsDto client)
    {
        return new ClientResponse
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
