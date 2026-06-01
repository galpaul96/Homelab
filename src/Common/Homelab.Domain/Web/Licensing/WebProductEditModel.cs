namespace Homelab.Domain.Web.Licensing;

public class WebProductEditModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Type { get; set; }
    public string? HostedOn { get; set; }
}
