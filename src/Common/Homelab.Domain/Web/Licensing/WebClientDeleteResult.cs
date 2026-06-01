namespace Homelab.Domain.Web.Licensing;

public class WebClientDeleteResult
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public bool Blocked { get; init; }
    public string? Message { get; init; }
}
