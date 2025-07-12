using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OpenIdProvider.Data;
using OpenIdProvider.Data.Models;

public class UserNavViewModel
{
    public List<Organization> UserOrganizations { get; set; } = new List<Organization>();
    public string? UserName { get; set; }
    public string? UserAvatarUrl { get; set; }
    public string? SelectedOrganizationSlug { get; set; }
}

public class UserNavViewComponent : ViewComponent
{

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UserNavViewComponent> _logger;

    public UserNavViewComponent(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, ILogger<UserNavViewComponent> logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync(string? selectedOrganizationSlug)
    {
        var model = new UserNavViewModel
        {
            SelectedOrganizationSlug = selectedOrganizationSlug
        };

        if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
        {
            if (User is not ClaimsPrincipal claimsPrincipal)
            {
                _logger.LogWarning("User is authenticated but ClaimsPrincipal is null.");
                return View(model);
            }

            // nullable check in Login/SignUp
            model.UserName = claimsPrincipal.FindFirst("DisplayName")!.Value;
            model.UserAvatarUrl = claimsPrincipal.FindFirst("AvatarUrl")!.Value;
            var primaryOrgIdClaim = claimsPrincipal.FindFirst("PrimaryOrganizationId");
            if (primaryOrgIdClaim != null && Guid.TryParse(primaryOrgIdClaim.Value, out Guid primaryOrgId))
            {
                var primaryOrganization = await _dbContext.Organizations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == primaryOrgId);

                if (primaryOrganization != null)
                {
                    model.UserOrganizations.Add(primaryOrganization);
                    model.SelectedOrganizationSlug = selectedOrganizationSlug ?? primaryOrganization.Slug;
                }
            }

            var userId = _userManager.GetUserId(claimsPrincipal);
            if (userId != null)
            {
                var userWithManagedOrgs = await _dbContext.Users
                    .Include(u => u.ManagedOrganizations)
                        .ThenInclude(mo => mo.Organization)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

                if (userWithManagedOrgs != null)
                {
                    model.UserOrganizations.AddRange(
                        userWithManagedOrgs
                        .ManagedOrganizations
                        .Select(mo => mo.Organization)
                        );
                }
            }
            model.UserOrganizations = model.UserOrganizations.DistinctBy(o => o.Id).ToList();
        }
        return View(model);
    }
}