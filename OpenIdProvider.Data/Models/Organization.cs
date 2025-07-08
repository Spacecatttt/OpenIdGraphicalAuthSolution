using System.ComponentModel.DataAnnotations;

namespace OpenIdProvider.Data.Models;

public class Organization
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Slug { get; set; } = string.Empty; // URL-friendly identifier

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
}
