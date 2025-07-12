using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

using OpenIdProvider.Data;
using OpenIdProvider.Data.Models;
using OpenIdProvider.Web.Services;
public class OrganizationsModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? OrgSlug { get; set; }

    public Organization? CurrentOrganization { get; set; }
    private readonly ILogger<OrganizationsModel> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IOrganizationResolver _organizationResolver;

    public OrganizationsModel(ILogger<OrganizationsModel> logger, ApplicationDbContext dbContext, IOrganizationResolver organizationResolver)
    {
        _logger = logger;
        _dbContext = dbContext;
        _organizationResolver = organizationResolver;
    }
    public async Task OnGetAsync()
    {
        if (!string.IsNullOrWhiteSpace(OrgSlug))
        {
            CurrentOrganization = await _dbContext.Organizations
                .FirstOrDefaultAsync(o => o.Slug == OrgSlug);
        }
    }
}
