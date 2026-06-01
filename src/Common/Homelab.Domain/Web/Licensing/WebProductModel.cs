namespace Homelab.Domain.Web.Licensing;

public class WebProductModel
{
    public Guid Id { get; set; }
    public Guid ExternalId { get; set; }
    public Guid ClientId { get; set; }
    public WebProductClientModel? Client { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? HostedOn { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
