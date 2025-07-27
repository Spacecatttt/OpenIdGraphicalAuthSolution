using Microsoft.EntityFrameworkCore;

namespace OpenIdProvider.Data.Models;

[PrimaryKey(nameof(OrganizationId), nameof(ClientId))]
public class ClientOwnership
{
    public Guid OrganizationId { get; set; }
    public int ClientId { get; set; }

    public virtual Organization Organization { get; set; } = null!;

    // We should have no navigation Property!
    // public virtual Client Client { get; set; }

    /// <summary>
    /// Determines if new, unknown users are allowed to sign up through this client.
    /// If true, a user not found in the database will be redirected to the registration page.
    /// If false, they will be shown a "User not found" error.
    /// </summary>
    public bool EnableSignup { get; set; } = false;
}