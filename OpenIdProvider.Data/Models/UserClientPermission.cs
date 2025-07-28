
namespace OpenIdProvider.Data.Models;

public class UserClientPermission
{
    public Guid UserId { get; set; }
    public virtual ApplicationUser User { get; set; }  = null!;
    public int ClientId { get; set; }
}