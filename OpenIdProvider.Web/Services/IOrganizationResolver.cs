using System.Security.Claims;
using OpenIdProvider.Data.Models;

namespace OpenIdProvider.Web.Services;

public interface IOrganizationResolver
{
    Task<Organization?> ResolvePrimaryOrganizationAsync(ClaimsPrincipal user);
}
