namespace OpenIdProvider.Data.Models;

/// <summary>
/// Represents a permission grant that allows all users belonging to a specific
/// organization to authenticate with a specific client.
/// This provides a broad, organization-level authorization mechanism,
/// complementing the more granular user-specific permissions in UserClientPermission.
/// </summary>
public class OrganizationClientPermission
{
    public Guid OrganizationId { get; set; }
    public virtual Organization Organization { get; set; } = null!;
    public int ClientId { get; set; }
}