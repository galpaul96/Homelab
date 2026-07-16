namespace Homelab.Web.Gateway;

public sealed class ApiClientOptions
{
    public const string SectionName = "ApiClient";

    public required Uri BaseAddress { get; init; }
}
