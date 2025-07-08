using Microsoft.AspNetCore.Identity;

namespace OpenIdProvider.Data.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    // Custom properties for graphical authentication
    public string? GraphicalPasswordHash { get; set; }
    public string? GraphicalAuthMethodType { get; set; } // e.g., "ImageSequence", "ClickPattern"
    public string? GraphicalAuthMetadata { get; set; }   // e.g., JSON storing image IDs or pattern data

    public Guid OrganizationId { get; set; }
    public virtual Organization Organization { get; set; } = null!;
    public virtual ICollection<UserGroup> Groups { get; set; } = new List<UserGroup>();
}