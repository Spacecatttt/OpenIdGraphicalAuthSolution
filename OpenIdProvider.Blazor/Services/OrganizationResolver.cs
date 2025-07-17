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

        var userIdString = user.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userGuid))
        {
            return null;
        }

        var appUser = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.PrimaryOrganization)
            .FirstOrDefaultAsync(u => u.Id == userGuid);

        return appUser?.PrimaryOrganization;
    }
}
