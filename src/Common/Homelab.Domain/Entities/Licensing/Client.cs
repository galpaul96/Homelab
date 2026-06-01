namespace Homelab.Domain.Entities.Licensing;

public class Client : Audit
{
    public Guid ExternalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Notes { get; set; }
}
