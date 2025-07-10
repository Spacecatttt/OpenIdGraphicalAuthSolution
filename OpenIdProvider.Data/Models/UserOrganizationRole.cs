using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OpenIdProvider.Data.Models;

// This is the join table for the Many-to-Many relationship
// between ApplicationUser and Organization for additional managed organizations.
public class UserOrganizationRole
{

    public Guid UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual ApplicationUser User { get; set; } = null!;

    public Guid OrganizationId { get; set; }
    [ForeignKey("OrganizationId")]
    public virtual Organization Organization { get; set; } = null!;

    // Role for this specific organization (e.g., "Admin", "Viewer", "Billing")
    public string Role { get; set; } = "Viewer";
    public DateTime AddedDate { get; set; } = DateTime.UtcNow;
}