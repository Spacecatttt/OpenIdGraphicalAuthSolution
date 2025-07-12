using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OpenIdProvider.Data;
using OpenIdProvider.Web.Services;

namespace OpenIdProvider.Web.Pages.Home
{
    [Authorize]
    public class DashboardModel : PageModel
    {

        private readonly ILogger<DashboardModel> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IOrganizationResolver _organizationResolver;

        [BindProperty(SupportsGet = true)]
        public string? OrgSlug { get; set; }
        public Data.Models.Organization? CurrentOrganization { get; private set; }

        [BindProperty]
        public int TotalUsers { get; set; }

        [BindProperty]
        public int NewUsersToday { get; set; }

        [BindProperty]
        public int NewUsersPast7Days { get; set; }

        [BindProperty]
        public int NewUsersPast30Days { get; set; }
        public string ChartDataJson { get; private set; }

        public DashboardModel(ILogger<DashboardModel> logger, ApplicationDbContext dbContext, IOrganizationResolver organizationResolver)
        {
            _logger = logger;
            _dbContext = dbContext;
            _organizationResolver = organizationResolver;
            ChartDataJson = string.Empty;
        }

        public async Task OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(OrgSlug))
            {
                var org = await _organizationResolver.ResolvePrimaryOrganizationAsync(User);
                if (org != null)
                {
                    OrgSlug = org.Slug;
                    CurrentOrganization = org;
                }
            }
            _logger.LogInformation("Dashboard accessed for organization: {OrgSlug}", OrgSlug);
            CurrentOrganization = await _dbContext.Organizations.FirstOrDefaultAsync(o => o.Slug == OrgSlug);

            if (CurrentOrganization == null)
            {
                Response.StatusCode = 404;
                return;
            }

            var today = DateTime.UtcNow.Date;
            var past7Days = today.AddDays(-7);
            var past30Days = today.AddDays(-30);

            TotalUsers = CurrentOrganization.ManagedByUsers.Count;
            NewUsersToday = CurrentOrganization.ManagedByUsers
                .Count(u => u.AddedDate.Date == today);
            NewUsersPast7Days = CurrentOrganization.ManagedByUsers
                .Count(u => u.AddedDate >= past7Days);
            NewUsersPast30Days = CurrentOrganization.ManagedByUsers
                .Count(u => u.AddedDate >= past30Days);

            // Generate data for graph for 30 days
            var chartData = new List<int>();
            for (int i = 29; i >= 0; i--)
            {
                var day = today.AddDays(-i);

                int countForDay = CurrentOrganization.ManagedByUsers
                    .Count(u => u.AddedDate.Date == day);

                chartData.Add(countForDay);
            }

            ChartDataJson = JsonSerializer.Serialize(chartData);
        }
    }
}
