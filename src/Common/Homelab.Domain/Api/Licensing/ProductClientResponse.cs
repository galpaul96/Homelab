namespace Homelab.Domain.Api.Licensing;

public class ProductClientResponse
{
    public Guid Id { get; set; }
    public Guid ExternalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
