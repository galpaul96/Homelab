using Homelab.Domain.Web.Licensing;

namespace Homelab.Web.Services
{
    public interface IClientService
    {
        Task<IReadOnlyCollection<WebClientModel>> GetClientsAsync();

        Task<WebClientModel?> GetClientAsync(Guid id);

        Task<WebClientModel> CreateClientAsync(WebClientDraft client);

        Task<WebClientModel?> UpdateClientAsync(Guid id, WebClientEditModel client);

        Task<bool> DeleteClientAsync(Guid id);
    }
}
