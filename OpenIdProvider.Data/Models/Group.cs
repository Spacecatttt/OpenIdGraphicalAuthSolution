using System.ComponentModel.DataAnnotations;

namespace OpenIdProvider.Data.Models;

// Represents a logical grouping of users within an Organization
public class Group
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string? Description { get; set; }

    public Guid OrganizationId { get; set; }
    public virtual Organization Organization { get; set; } = null!;

    public virtual ICollection<UserGroup> Users { get; set; } = new List<UserGroup>();

    // Claims assigned directly to this group
    public virtual ICollection<GroupClaim> Claims { get; set; } = new List<GroupClaim>();
}