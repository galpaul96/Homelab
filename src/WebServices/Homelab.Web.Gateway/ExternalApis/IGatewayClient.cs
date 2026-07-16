using System.Net.Http;

namespace Homelab.Web.Gateway.ExternalApis;

public interface IGatewayClient
{
    Task<HttpResponseMessage> GetAsync(string route, CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> PostAsync(string route, object? payload = null, CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> PutAsync(string route, object? payload = null, CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> DeleteAsync(string route, object? payload = null, CancellationToken cancellationToken = default);
}
