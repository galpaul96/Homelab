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

    public async Task<HttpResponseMessage> GetAsync(string baseUrl, string route)
    {
        using var httpClient = _httpClientFactory.CreateClient(nameof(GetAsync));

        var uri = BuildUri(baseUrl, route);
        return await httpClient.GetAsync(uri);
    }

    public async Task<HttpResponseMessage> PostAsync(string baseUrl, string route, object? payload = null)
    {
        using var httpClient = _httpClientFactory.CreateClient(nameof(GetAsync));

        var uri = BuildUri(baseUrl, route);
        return await httpClient.PostAsync(uri, CreateJsonContent(payload));
    }

    public Task<HttpResponseMessage> PatchAsync(string baseUrl, string route, object? payload = null)
    {
        throw new NotImplementedException("PatchAsync is not implemented yet.");
    }

    public async Task<HttpResponseMessage> PutAsync(string baseUrl, string route, object? payload = null)
    {
        using var httpClient = _httpClientFactory.CreateClient(nameof(GetAsync));

        var uri = BuildUri(baseUrl, route);
        return await httpClient.PutAsync(uri, CreateJsonContent(payload));
    }

    public async Task<HttpResponseMessage> DeleteAsync(string baseUrl, string route, object? payload = null)
    {
        using var httpClient = _httpClientFactory.CreateClient(nameof(GetAsync));

        var uri = BuildUri(baseUrl, route);
        if (payload is null)
        {
            return await httpClient.DeleteAsync(uri);
        }

        using var request = new HttpRequestMessage(HttpMethod.Delete, uri)
        {
            Content = CreateJsonContent(payload)
        };

        return await httpClient.SendAsync(request);
    }

    private static Uri BuildUri(string baseUrl, string route)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(route);

        var normalizedBaseUrl = baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/";
        var normalizedRoute = route.StartsWith('/') ? route[1..] : route;

        return new Uri(new Uri(normalizedBaseUrl), normalizedRoute);
    }

    private static StringContent CreateJsonContent(object? payload)
    {
        return new StringContent(JsonSerializer.Serialize(payload, JsonSerializerOptions), Encoding.UTF8, "application/json");
    }
}
