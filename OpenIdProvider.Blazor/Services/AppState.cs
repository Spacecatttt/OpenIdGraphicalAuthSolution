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
        var userPrincipal = authState.User;

        if (userPrincipal.Identity?.IsAuthenticated ?? false)
        {
            var userId = _userManager.GetUserId(userPrincipal);
            if (userId != null)
            {
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

                var userWithData = await dbContext.Users
                    .Include(u => u.PrimaryOrganization)
                    .Include(u => u.ManagedOrganizations)
                        .ThenInclude(mo => mo.Organization)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

                if (userWithData != null)
                {
                    CurrentUser = userWithData;

                    var orgs = new List<Organization>();
                    if (userWithData.PrimaryOrganization != null)
                    {
                        orgs.Add(userWithData.PrimaryOrganization);
                    }
                    orgs.AddRange(userWithData.ManagedOrganizations.Select(mo => mo.Organization));
                    UserOrganizations = orgs.DistinctBy(o => o.Id).ToList();

                    var currentSlug = SelectedOrganization?.Slug;
                    if (currentSlug == null || !UserOrganizations.Any(o => o.Slug == currentSlug))
                    {
                        SelectedOrganization = userWithData.PrimaryOrganization ?? UserOrganizations.FirstOrDefault();
                    }
                }
            }
            _isInitialized = true;
            NotifyStateChanged();
        }
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