using AutoMapper;
using Homelab.Api.Ef;
using Homelab.Domain.Entities.Licensing;
using Homelab.Domain.Services.Licensing;
using Microsoft.EntityFrameworkCore;

namespace Homelab.Api.Services;

internal class ClientService : IClientService
{
    private readonly IRepository _repository;
    private readonly IMapper _mapper;

    public ClientService(
        IRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyCollection<ClientDetailsDto>> GetClientsAsync(
        CancellationToken cancellationToken = default)
    {
        var clients = await _repository.GetAllAsync<Client>()
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ThenBy(x => x.CreatedDate)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyCollection<ClientDetailsDto>>(clients);
    }

    public async Task<ClientDetailsDto?> GetClientAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var client = await _repository.GetAllAsync<Client>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return client is null
            ? null
            : _mapper.Map<ClientDetailsDto>(client);
    }

    public async Task<ClientDetailsDto> CreateClientAsync(
        CreateClientDto request,
        CancellationToken cancellationToken = default)
    {
        var name = TrimToNull(request.Name);
        if (name is null)
        {
            throw new ArgumentException("Client name is required.", nameof(request));
        }

        var now = DateTime.UtcNow;
        var client = _mapper.Map<Client>(request);
        client.Name = name;
        client.Description = TrimToNull(request.Description);
        client.Notes = TrimToNull(request.Notes);
        client.CreatedDate = now;
        client.UpdatedDate = now;

        var createdClient = await _repository.AddAsync(client);

        return _mapper.Map<ClientDetailsDto>(createdClient);
    }

    public async Task<ClientDetailsDto?> UpdateClientAsync(
        UpdateClientDto request,
        CancellationToken cancellationToken = default)
    {
        var client = await _repository.GetAllAsync<Client>()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (client is null)
        {
            return null;
        }

        client.Description = TrimToNull(request.Description);
        client.Notes = TrimToNull(request.Notes);

        await _repository.UpdateAsync(client);

        return _mapper.Map<ClientDetailsDto>(client);
    }

    public async Task<ClientDeleteResultDto> DeleteClientAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var client = await _repository.GetAllAsync<Client>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (client is null)
        {
            return ClientDeleteResultDto.Missing();
        }

        var productCount = await _repository.GetAllAsync<Product>()
            .CountAsync(x => x.ClientId == id, cancellationToken);

        if (productCount > 0)
        {
            return ClientDeleteResultDto.Blocked(productCount);
        }

        await _repository.DeleteAsync<Client>(id);
        return ClientDeleteResultDto.Deleted();
    }

    private static string? TrimToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
