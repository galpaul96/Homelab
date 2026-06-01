namespace Homelab.Domain.Api.Licensing;

public class UpdateProductRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Type { get; init; }
    public string? HostedOn { get; init; }
}
