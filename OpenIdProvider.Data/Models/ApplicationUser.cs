using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace OpenIdProvider.Data.Models;

public class ApplicationUser : IdentityUser<Guid>
{

    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    // Custom properties for graphical authentication
    public string? GraphicalPasswordHash { get; set; }
    public string? GraphicalAuthMethodType { get; set; } // e.g., "ImageSequence", "ClickPattern"
    public string? GraphicalAuthMetadata { get; set; }   // e.g., JSON storing image IDs or pattern data

    public Guid PrimaryOrganizationId { get; set; }

    [ForeignKey("PrimaryOrganizationId")]
    public virtual Organization PrimaryOrganization { get; set; } = null!;
    public virtual ICollection<UserGroup> Groups { get; set; } = new List<UserGroup>();

    // This collection represents additional organizations a user might manage
    // or have a role in, beyond their PrimaryOrganization.
    public virtual ICollection<UserOrganizationRole> ManagedOrganizations { get; set; } = new List<UserOrganizationRole>();

}