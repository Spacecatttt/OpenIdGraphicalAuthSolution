using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenIdProvider.Data;
using OpenIdProvider.Web.Services;
using OrganizationData = OpenIdProvider.Data.Models.Organization;

namespace OpenIdProvider.Web.Pages.Organization
{
    public class OrganizationsModel : PageModel
    {
        private readonly ILogger<OrganizationsModel> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IOrganizationResolver _organizationResolver;

        public OrganizationsModel(ILogger<OrganizationsModel> logger, ApplicationDbContext dbContext, IOrganizationResolver organizationResolver)
        {
            _logger = logger;
            _dbContext = dbContext;
            _organizationResolver = organizationResolver;
        }

        // Properties for the list view
        public List<OrganizationData> Organizations { get; set; } = new List<OrganizationData>();

        [BindProperty(SupportsGet = true)]
        public string? NameSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SlugSearch { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int NumberItemsPageSize { get; set; } = 10;

        public int TotalPages { get; set; }
        public int TotalOrganizations { get; set; }
        public string NextSortOrder { get; private set; }

        // Retaining OrgSlug and CurrentOrganization if a detail view is intended

        [BindProperty(SupportsGet = true)]

        public string? OrgSlug { get; set; }

        public OrganizationData? CurrentOrganization { get; set; }

        public async Task<PartialViewResult> OnGetTableDataAsync()
        {
            // Ця логіка повністю дублює OnGetAsync, але повертає PartialView
            IQueryable<OrganizationData> organizationsQuery = _dbContext.Organizations;

            if (!string.IsNullOrWhiteSpace(NameSearch))
            {
                organizationsQuery = organizationsQuery.Where(o => o.Name.Contains(NameSearch));
            }
            if (!string.IsNullOrWhiteSpace(SlugSearch))
            {
                organizationsQuery = organizationsQuery.Where(o => o.Slug.Contains(SlugSearch));
            }

            switch (SortOrder)
            {
                case "date_asc":
                    organizationsQuery = organizationsQuery.OrderBy(o => o.CreatedDate);
                    NextSortOrder = "date_desc";
                    break;
                case "date_desc":
                    organizationsQuery = organizationsQuery.OrderByDescending(o => o.CreatedDate);
                    NextSortOrder = "";
                    break;
                default:
                    organizationsQuery = organizationsQuery.OrderBy(o => o.Name);
                    NextSortOrder = "date_asc";
                    break;
            }

            TotalOrganizations = await organizationsQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalOrganizations / (double)NumberItemsPageSize);
            if (TotalPages == 0) { TotalPages = 1; }
            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;

            Organizations = await organizationsQuery
                .Skip((CurrentPage - 1) * NumberItemsPageSize)
                .Take(NumberItemsPageSize)
                .ToListAsync();

            // Повертаємо тільки Partial View з оновленими даними
            return Partial("_OrganizationsTable", this);
        }
        public IActionResult OnPostAdd()
        {
            return RedirectToPage("/Organizations/Add");
        }

        public IActionResult OnPostEdit(Guid id)
        {
            return RedirectToPage("/Organizations/Edit", new { id = id });
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            _logger.LogInformation($"Delete organization action triggered for ID: {id}");
            var organizationToDelete = await _dbContext.Organizations.FindAsync(id);
            if (organizationToDelete != null)
            {
                _dbContext.Organizations.Remove(organizationToDelete);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Organization with ID {id} deleted successfully.");
            }
            else
            {
                _logger.LogWarning($"Attempted to delete non-existent organization with ID: {id}");
            }
            // Redirect back to the list page, preserving query string for filters/pagination
            return RedirectToPage(new
            {
                NameSearch,
                SlugSearch,
                SortOrder,
                CurrentPage,
                NumberItemsPageSize
            });
        }

        public IActionResult OnPostGroups(Guid id)
        {
            return RedirectToPage("/Organizations/Groups", new { orgId = id });
        }

        public IActionResult OnPostUsers(Guid id)
        {
            return RedirectToPage("/Organizations/Users", new { orgId = id });
        }
    }
}