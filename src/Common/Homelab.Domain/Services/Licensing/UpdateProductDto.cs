namespace Homelab.Domain.Services.Licensing;

public class UpdateProductDto
{
    public Guid Id { get; set; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Type { get; init; }
    public string? HostedOn { get; init; }
}
