using System.ComponentModel.DataAnnotations;

namespace OpenIdProvider.Data.Models;

// Join entity for the many-to-many relationship between ApplicationUser and Group
public class UserGroup
{
    public Guid ApplicationUserId { get; set; }
    public Guid GroupId { get; set; }

    // Navigation Properties
    public virtual ApplicationUser ApplicationUser { get; set; } = null!;
    public virtual Group Group { get; set; } = null!;

    // Optional payload property
    public DateTime AssignedDate { get; set; }
}