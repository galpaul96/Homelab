using Homelab.Api.Controllers;
using Homelab.Api.Services;
using Homelab.Domain.Api.Licensing;
using Homelab.Domain.Services.Licensing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Homelab.Tests;

public class LicensingControllerTests
{
    [Fact]
    public async Task GetClientAsyncReturnsNotFoundWhenClientDoesNotExist()
    {
        var controller = CreateController(new FakeClientService());

        var result = await controller.GetClientAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateClientAsyncReturnsCreatedClient()
    {
        var clientId = Guid.NewGuid();
        var service = new FakeClientService
        {
            CreateClient = request => new ClientDetailsDto
            {
                Id = clientId,
                ExternalId = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Notes = request.Notes
            }
        };
        var controller = CreateController(service);

        var result = await controller.CreateClientAsync(
            new CreateClientRequest
            {
                Name = "Acme",
                Description = "External client",
                Notes = "Important notes"
            },
            TestContext.Current.CancellationToken);

        var created = Assert.IsType<CreatedAtRouteResult>(result.Result);
        Assert.Equal("GetLicensingClient", created.RouteName);

        var response = Assert.IsType<ClientResponse>(created.Value);
        Assert.Equal(clientId, response.Id);
        Assert.Equal("Acme", response.Name);
        Assert.Equal("External client", response.Description);
        Assert.Equal("Important notes", response.Notes);
    }

    [Fact]
    public async Task UpdateClientAsyncReturnsNotFoundWhenClientDoesNotExist()
    {
        var controller = CreateController(new FakeClientService());

        var result = await controller.UpdateClientAsync(
            Guid.NewGuid(),
            new UpdateClientRequest { Description = "Updated" },
            TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteClientAsyncReturnsNoContentWhenClientIsDeleted()
    {
        var service = new FakeClientService
        {
            DeleteClient = _ => true
        };
        var controller = CreateController(service);

        var result = await controller.DeleteClientAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.IsType<NoContentResult>(result);
    }

    private static LicensingController CreateController(FakeClientService service)
    {
        return new LicensingController(
            service,
            NullLogger<LicensingController>.Instance);
    }

    private sealed class FakeClientService : IClientService
    {
        public Func<CreateClientDto, ClientDetailsDto>? CreateClient { get; init; }

        public Func<Guid, bool>? DeleteClient { get; init; }

        public Task<IReadOnlyCollection<ClientDetailsDto>> GetClientsAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<ClientDetailsDto>>([]);
        }

        public Task<ClientDetailsDto?> GetClientAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ClientDetailsDto?>(null);
        }

        public Task<ClientDetailsDto> CreateClientAsync(
            CreateClientDto client,
            CancellationToken cancellationToken = default)
        {
            if (CreateClient is not null)
            {
                return Task.FromResult(CreateClient(client));
            }

            throw new ArgumentException("Client name is required.", nameof(client));
        }

        public Task<ClientDetailsDto?> UpdateClientAsync(
            UpdateClientDto client,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ClientDetailsDto?>(null);
        }

        public Task<bool> DeleteClientAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeleteClient?.Invoke(id) ?? false);
        }
    }
}
