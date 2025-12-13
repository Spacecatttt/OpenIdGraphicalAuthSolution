using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIdProvider.Data;
using OpenIdProvider.Data.Models;

public class ManagerUserRequirement : IAuthorizationRequirement { }

public class ManagerUserHandler : AuthorizationHandler<ManagerUserRequirement>
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly UserManager<ApplicationUser> _userManager;

    public ManagerUserHandler(IDbContextFactory<ApplicationDbContext> dbContextFactory, UserManager<ApplicationUser> userManager)
    {
        _dbContextFactory = dbContextFactory;
        _userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ManagerUserRequirement requirement)
    {

        var userIdString = _userManager.GetUserId(context.User);

        if (!Guid.TryParse(userIdString, out var userId))
        {
            return; // User ID is not a valid Guid or not found
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var user = await dbContext.Users.FindAsync(userId);

        if (user?.PrimaryOrganizationId != null)
        {
            context.Succeed(requirement);
        }

        // In case if Manager no need to have own Organization
        var isManagerAnywhere = await dbContext.UserOrganizationRoles
        .AsNoTracking()
        .AnyAsync(role => role.UserId == userId && role.Role >= OrganizationRole.Viewer);

        if (isManagerAnywhere)
        {
            context.Succeed(requirement);
            return;
        }
    }
}