using System.ComponentModel.DataAnnotations;

namespace OpenIdProvider.Data.Models;
// Stores claims assigned directly to a Group
public class GroupClaim
{
    [Key]
    public int Id { get; set; }

    public Guid GroupId { get; set; }
    public virtual Group Group { get; set; } = null!;

    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;
}