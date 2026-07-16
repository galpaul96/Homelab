using System.ComponentModel.DataAnnotations;

namespace Homelab.Web.IdentityAdministration;

public sealed class IdentityAdministrationOptions
{
    public const string SectionName = "IdentityAdministration";

    [Required, EmailAddress]
    public string DefaultAdminEmail { get; init; } = "admin@homelab.local";

    public string DefaultAdminPassword { get; init; } = string.Empty;

    [Required, StringLength(256, MinimumLength = 1)]
    public string AdminRoleName { get; init; } = "Admin";

    public bool SeedDefaultAdministrator { get; init; } = true;

    [Range(1, 3650)]
    public int AuditRetentionDays { get; init; } = 365;
}
