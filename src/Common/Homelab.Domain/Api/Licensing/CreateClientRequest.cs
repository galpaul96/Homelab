namespace Homelab.Domain.Api.Licensing;

public class CreateClientRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Notes { get; init; }
}
