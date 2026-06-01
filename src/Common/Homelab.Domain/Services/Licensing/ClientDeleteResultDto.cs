namespace Homelab.Domain.Services.Licensing;

public class ClientDeleteResultDto
{
    public bool Succeeded { get; init; }
    public bool NotFound { get; init; }
    public bool BlockedByProducts { get; init; }
    public int ProductCount { get; init; }
    public string? Message { get; init; }

    public static ClientDeleteResultDto Deleted() => new() { Succeeded = true };

    public static ClientDeleteResultDto Missing() => new()
    {
        NotFound = true,
        Message = "Client was not found."
    };

    public static ClientDeleteResultDto Blocked(int productCount) => new()
    {
        BlockedByProducts = true,
        ProductCount = productCount,
        Message = "Delete this client's products before deleting the client."
    };
}
