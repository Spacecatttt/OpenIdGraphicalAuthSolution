using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenIdProvider.Data;
using OpenIdProvider.Data.Models;

namespace OpenIdProvider.Blazor.Services;

public class OrganizationResolver : IOrganizationResolver
{
    private readonly ApplicationDbContext _dbContext;

    public OrganizationResolver(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Organization?> ResolvePrimaryOrganizationAsync(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var primaryOrgIdClaim = user.FindFirst("PrimaryOrganizationId");
        if (primaryOrgIdClaim == null)
            return null;

        if (!Guid.TryParse(primaryOrgIdClaim.Value, out Guid primaryOrgId))
            return null;

        return await _dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == primaryOrgId);
    }
}
