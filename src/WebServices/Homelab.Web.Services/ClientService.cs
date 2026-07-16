using Homelab.Domain.Api.Licensing;
using Homelab.Domain.Web.Licensing;
using Homelab.Web.Gateway.ExternalApis;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;

namespace Homelab.Web.Services
{
    internal class ClientService : IClientService
    {
        private readonly ILogger<ClientService> logger;
        private readonly IGatewayClient gatewayClient;

        public ClientService(
            ILogger<ClientService> logger,
            IGatewayClient gatewayClient)
        {
            this.logger = logger;
            this.gatewayClient = gatewayClient;
        }

        public async Task<IReadOnlyCollection<WebClientModel>> GetClientsAsync()
        {
            logger.LogInformation("Getting clients.");

            var response = await gatewayClient.GetAsync("Licensing/clients");
            var clients = await ReadResponseAsync<IReadOnlyCollection<ClientResponse>>(
                response,
                "clients") ?? [];

            return clients.Select(MapClient).ToArray();
        }

        public async Task<WebClientModel?> GetClientAsync(Guid id)
        {
            logger.LogInformation("Getting client {ClientId}.", id);

            var response = await gatewayClient.GetAsync($"Licensing/clients/{id}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation("Client {ClientId} was not found.", id);
                return null;
            }

            var client = await ReadResponseAsync<ClientResponse>(response, "client");
            return client is null ? null : MapClient(client);
        }

        public async Task<WebClientModel> CreateClientAsync(WebClientDraft client)
        {
            logger.LogInformation("Creating client {ClientName}.", client.Name);

            var response = await gatewayClient.PostAsync(
                "Licensing/clients",
                new CreateClientRequest
                {
                    Name = client.Name,
                    Description = client.Description,
                    Notes = client.Notes
                });

            var createdClient = await ReadResponseAsync<ClientResponse>(response, "created client")
                ?? throw new InvalidOperationException("The licensing API returned an empty client response.");

            return MapClient(createdClient);
        }

        public async Task<WebClientModel?> UpdateClientAsync(Guid id, WebClientEditModel client)
        {
            logger.LogInformation("Updating client {ClientId}.", id);

            var response = await gatewayClient.PutAsync(
                $"Licensing/clients/{id}",
                new UpdateClientRequest
                {
                    Description = client.Description,
                    Notes = client.Notes
                });

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation("Client {ClientId} was not found for update.", id);
                return null;
            }

            var updatedClient = await ReadResponseAsync<ClientResponse>(response, "updated client");
            return updatedClient is null ? null : MapClient(updatedClient);
        }

        public async Task<WebClientDeleteResult> DeleteClientAsync(Guid id)
        {
            logger.LogInformation("Deleting client {ClientId}.", id);

            var response = await gatewayClient.DeleteAsync($"Licensing/clients/{id}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation("Client {ClientId} was not found for deletion.", id);
                return new WebClientDeleteResult
                {
                    NotFound = true,
                    Message = "Client was not found."
                };
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
                return new WebClientDeleteResult
                {
                    Blocked = true,
                    Message = problem?.Detail ?? "Delete this client's products before deleting the client."
                };
            }

            if (response.IsSuccessStatusCode)
            {
                return new WebClientDeleteResult { Succeeded = true };
            }

            var body = await response.Content.ReadAsStringAsync();
            logger.LogError(
                "Failed to delete client. Status code: {StatusCode}. Response: {ResponseBody}",
                response.StatusCode,
                body);

            throw new Exception($"Failed to delete client. Status code: {response.StatusCode}");
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

        private static WebClientModel MapClient(ClientResponse client)
        {
            return new WebClientModel
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

        private sealed class ProblemResponse
        {
            public string? Detail { get; set; }
        }
    }
}
