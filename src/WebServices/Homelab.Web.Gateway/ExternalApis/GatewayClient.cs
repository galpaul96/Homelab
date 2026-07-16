using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Homelab.Web.Gateway.ExternalApis;

internal class GatewayClient : IGatewayClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public GatewayClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory; // injecting the factory
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<HttpResponseMessage> GetAsync(string route, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient("api");
        return await httpClient.GetAsync(route, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsync(string route, object? payload = null, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient("api");
        return await httpClient.PostAsync(route, CreateJsonContent(payload), cancellationToken);
    }

    public async Task<HttpResponseMessage> PutAsync(string route, object? payload = null, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient("api");
        return await httpClient.PutAsync(route, CreateJsonContent(payload), cancellationToken);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string route, object? payload = null, CancellationToken cancellationToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient("api");
        if (payload is null)
        {
            return await httpClient.DeleteAsync(route, cancellationToken);
        }

        using var request = new HttpRequestMessage(HttpMethod.Delete, route)
        {
            Content = CreateJsonContent(payload)
        };

        return await httpClient.SendAsync(request, cancellationToken);
    }

    private static StringContent CreateJsonContent(object? payload)
    {
        return new StringContent(JsonSerializer.Serialize(payload, JsonSerializerOptions), Encoding.UTF8, "application/json");
    }
}
