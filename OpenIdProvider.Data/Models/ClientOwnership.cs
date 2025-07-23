using Microsoft.EntityFrameworkCore;

namespace OpenIdProvider.Data.Models;

[PrimaryKey(nameof(OrganizationId), nameof(ClientId))]
public class ClientOwnership
{
    public Guid OrganizationId { get; set; }
    public int ClientId { get; set; }

    public virtual required Organization Organization { get; set; }

    // We should have no navigation Property!
    // public virtual Client Client { get; set; }
}