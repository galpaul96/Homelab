using Microsoft.AspNetCore.Identity;
using Homelab.Domain.Entities.Web;

namespace Homelab.Web.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public ICollection<UserNotification> Notifications { get; set; } = [];
}

