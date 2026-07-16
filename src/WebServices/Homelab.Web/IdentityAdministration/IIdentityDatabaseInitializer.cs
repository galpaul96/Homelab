namespace Homelab.Web.IdentityAdministration;

public interface IIdentityDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
