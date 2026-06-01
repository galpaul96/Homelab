namespace Homelab.Domain.Services.Licensing;

public class CreateClientDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Notes { get; init; }
}
