
namespace OpenIdProvider.Data.Models;

public class OrganizationClientPermission
{
    public Guid OrganizationId { get; set; }
    public virtual Organization Organization { get; set; } = null!;
    public int ClientId { get; set; }
}