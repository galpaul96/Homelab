namespace Homelab.Domain.Services.Licensing;

public class UpdateClientDto
{
    public Guid Id { get; set; }
    public string? Description { get; init; }
    public string? Notes { get; init; }
}
