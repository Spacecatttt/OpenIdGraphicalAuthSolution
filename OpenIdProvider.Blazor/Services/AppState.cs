using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIdProvider.Data;
using OpenIdProvider.Data.Models;

public class AppState : IDisposable
{
    public bool IsLightTheme { get; private set; } = true;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public ApplicationUser? CurrentUser { get; private set; }
    public List<Organization> UserOrganizations { get; private set; } = new();
    public Organization? SelectedOrganization { get; private set; }


    public event Action? OnChange;
    private bool _isInitialized = false;

    public AppState(IDbContextFactory<ApplicationDbContext> dbContextFactory,
     UserManager<ApplicationUser> userManager,
     AuthenticationStateProvider authenticationStateProvider)
    {
        _dbContextFactory = dbContextFactory;
        _userManager = userManager;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task InitializeAsync(bool forceRefresh = false)
    {
        if (_isInitialized && !forceRefresh) return;

        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated ?? false)
        {
            CurrentUser = await _userManager.GetUserAsync(user);
            if (CurrentUser != null)
            {
                await LoadUserOrganizationsAsync(CurrentUser);
                var currentSlug = SelectedOrganization?.Slug;
                if (currentSlug == null || !UserOrganizations.Any(o => o.Slug == currentSlug))
                {
                    SelectedOrganization = CurrentUser.PrimaryOrganization ?? UserOrganizations.FirstOrDefault();
                }
            }
        }
        _isInitialized = true;
        NotifyStateChanged();
    }


    // Called by the [JSInvokable] method in UserNav.razor
    public void SetTheme(bool isLight)
    {
        if (IsLightTheme != isLight)
        {
            IsLightTheme = isLight;
            NotifyStateChanged();
        }
    }

    private async Task LoadUserOrganizationsAsync(ApplicationUser user)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var userWithOrgs = await dbContext.Users
            .Include(u => u.PrimaryOrganization)
            .Include(u => u.ManagedOrganizations).ThenInclude(mo => mo.Organization)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        if (userWithOrgs == null) return;

        var orgs = new List<Organization>();
        if (userWithOrgs.PrimaryOrganization != null)
        {
            orgs.Add(userWithOrgs.PrimaryOrganization);
        }
        orgs.AddRange(userWithOrgs.ManagedOrganizations.Select(mo => mo.Organization));
        UserOrganizations = orgs.DistinctBy(o => o.Id).ToList();
    }

    public void SetSelectedOrganization(string orgSlug)
    {
        var newOrg = UserOrganizations.FirstOrDefault(o => o.Slug == orgSlug);
        if (newOrg != null && newOrg.Id != SelectedOrganization?.Id)
        {
            SelectedOrganization = newOrg;
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();

    public void Dispose()
    {
    }
}