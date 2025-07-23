using System.ComponentModel.DataAnnotations;
using Duende.IdentityServer.EntityFramework.Entities;

namespace OpenIdProvider.Data.Models;

public class Organization
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    // The Slug is unique
    public string Slug { get; set; } = string.Empty; // URL-friendly identifier

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    public virtual ICollection<ApplicationUser> PrimaryUsers { get; set; } = new List<ApplicationUser>();
    public virtual ICollection<UserOrganizationRole> ManagedByUsers { get; set; } = new List<UserOrganizationRole>();
    public virtual ICollection<OrganizationClientPermission> AllowedClientPermissions { get; set; } = new List<OrganizationClientPermission>();

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
    public virtual ICollection<ClientOwnership> OwnedClients { get; set; } = new List<ClientOwnership>();
}