using Homelab.Domain.Services.Licensing;

namespace Homelab.Api.Services;

public interface IClientService
{
    Task<IReadOnlyCollection<ClientDetailsDto>> GetClientsAsync(
        CancellationToken cancellationToken = default);

    Task<ClientDetailsDto?> GetClientAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ClientDetailsDto> CreateClientAsync(
        CreateClientDto client,
        CancellationToken cancellationToken = default);

    Task<ClientDetailsDto?> UpdateClientAsync(
        UpdateClientDto client,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteClientAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
