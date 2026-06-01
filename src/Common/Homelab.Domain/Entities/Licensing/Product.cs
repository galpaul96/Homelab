namespace Homelab.Domain.Entities.Licensing;

public class Product : Audit
{
    public Guid ExternalId { get; set; }
    public Guid ClientId { get; set; }
    public Client? Client { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? HostedOn { get; set; }
}
