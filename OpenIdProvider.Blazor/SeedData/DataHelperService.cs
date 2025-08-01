using Bogus;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIdProvider.Blazor.Components;
using OpenIdProvider.Data;
using OpenIdProvider.Data.Models;
using ClientEntity = Duende.IdentityServer.EntityFramework.Entities.Client;

namespace OpenIdProvider.Blazor.Services;

/// <summary>
/// A helper service to perform common data manipulation tasks for development and testing.
/// Unlike DatabaseSeeder, this service is designed to be used on a database that already contains data.
/// </summary>
/// <usage>
/// using (var scope = app.Services.CreateScope())
/// {
///     var services = scope.ServiceProvider;
///     var appDbContextFactory = services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
///     var configDbContextFactory = services.GetRequiredService<IDbContextFactory<ConfigurationDbContext>>();
///     var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
///     var logger = services.GetRequiredService<ILogger<DataHelperService>>();
///     var dataHelper = new DataHelperService(appDbContextFactory, configDbContextFactory, userManager, logger);
///  }
/// </usage>
public class DataHelperService
{
    private readonly IDbContextFactory<ApplicationDbContext> _appDbContextFactory;
    private readonly IDbContextFactory<ConfigurationDbContext> _configDbContextFactory;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DataHelperService> _logger;
    private const string Password = "Password123!";


    public DataHelperService(
        IDbContextFactory<ApplicationDbContext> appDbContextFactory,
        IDbContextFactory<ConfigurationDbContext> configDbContextFactory,
        UserManager<ApplicationUser> userManager,
        ILogger<DataHelperService> logger)
    {
        _appDbContextFactory = appDbContextFactory;
        _configDbContextFactory = configDbContextFactory;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new organization and assigns the specified user as its Owner.
    /// </summary>
    /// <param name="userIdentifier">The email or username of the user.</param>
    /// <returns>The created Organization, or null if the user was not found.</returns>
    /// <usage>
    /// var newOrganization = await _dataHelper.CreateOrganizationForUserAsync("owner1@example.com");
    /// </usage>
    public async Task<Organization?> CreateOrganizationForUserAsync(string userIdentifier)
    {
        var user = await FindUserByIdentifierAsync(userIdentifier);
        if (user == null)
        {
            _logger.LogWarning("Could not create organization: User '{UserIdentifier}' not found.", userIdentifier);
            return null;
        }

        await using var dbContext = await _appDbContextFactory.CreateDbContextAsync();
        var faker = new Faker();

        var newOrg = new Organization
        {
            Name = $"{user.DisplayName}'s New Organization",
            Slug = $"{user.UserName}-{faker.Random.Hexadecimal(6)}",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            IsActive = true
        };

        var userRole = new UserOrganizationRole
        {
            UserId = user.Id,
            Organization = newOrg,
            Role = OrganizationRole.Owner
        };

        dbContext.Organizations.Add(newOrg);
        dbContext.UserOrganizationRoles.Add(userRole);

        await dbContext.SaveChangesAsync();
        _logger.LogInformation("Successfully created organization '{OrgName}' for user '{UserName}'.", newOrg.Name, user.UserName);
        return newOrg;
    }

    /// <summary>
    /// Adds a specified number of randomly generated users to an organization.
    /// </summary>
    /// <param name="organizationId">The ID of the target organization.</param>
    /// <param name="count">The number of users to create.</param>
    /// <returns>A list of the newly created users, or an empty list if the organization was not found.</returns>
    /// <usage>
    /// var addedUsers = await _dataHelper.AddUsersToOrganizationAsync(newOrganization.Id, 5);
    /// </usage>
    public async Task<List<ApplicationUser>> AddUsersToOrganizationAsync(Guid organizationId, int count)
    {
        await using var dbContext = await _appDbContextFactory.CreateDbContextAsync();
        if (!await dbContext.Organizations.AnyAsync(o => o.Id == organizationId))
        {
            _logger.LogWarning("Could not add users: Organization with ID '{OrgId}' not found.", organizationId);
            return new List<ApplicationUser>();
        }

        var userFaker = new Faker<ApplicationUser>()
            .RuleFor(u => u.DisplayName, f => f.Name.FullName())
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.DisplayName, f.Random.Hexadecimal(4)))
            .RuleFor(u => u.UserName, (f, u) => u.Email)
            .RuleFor(u => u.EmailConfirmed, true);

        var newUsers = new List<ApplicationUser>();
        var faker = new Faker();
        for (int i = 0; i < count; i++)
        {
            var user = userFaker.Generate();
            var result = await _userManager.CreateAsync(user, Password);

            if (result.Succeeded)
            {
                var userRole = new UserOrganizationRole
                {
                    UserId = user.Id,
                    OrganizationId = organizationId,
                    Role = OrganizationRole.Member,
                    AddedDate = faker.Date.Recent(30, DateTime.UtcNow)
                };
                dbContext.UserOrganizationRoles.Add(userRole);
                newUsers.Add(user);
            }
            else
            {
                _logger.LogWarning("Failed to create user {UserName}: {Errors}", user.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        await dbContext.SaveChangesAsync();
        _logger.LogInformation("Successfully added {UserCount} new users to organization {OrgId}.", newUsers.Count, organizationId);
        return newUsers;
    }

    /// <summary>
    /// Adds a specified number of randomly generated groups to an organization.
    /// </summary>
    /// <usage>
    /// var addedGroups = await _dataHelper.AddGroupsToOrganizationAsync(newOrganization.Id, 3);
    /// </usage>
    public async Task<List<Group>> AddGroupsToOrganizationAsync(Guid organizationId, int count)
    {
        await using var dbContext = await _appDbContextFactory.CreateDbContextAsync();
        if (!await dbContext.Organizations.AnyAsync(o => o.Id == organizationId))
        {
            _logger.LogWarning("Could not add groups: Organization with ID '{OrgId}' not found.", organizationId);
            return new List<Group>();
        }

        var groupFaker = new Faker<Group>()
            .RuleFor(g => g.Name, f => f.Commerce.Department() + " " + f.Random.Word())
            .RuleFor(g => g.Description, f => f.Lorem.Sentence())
            .RuleFor(g => g.OrganizationId, organizationId);

        var newGroups = groupFaker.Generate(count);

        await dbContext.Groups.AddRangeAsync(newGroups);
        await dbContext.SaveChangesAsync();

        _logger.LogInformation("Successfully added {GroupCount} new groups to organization {OrgId}.", newGroups.Count, organizationId);
        return newGroups;
    }

    /// <summary>
    /// Adds a specified number of randomly generated clients and links them to an organization.
    /// </summary>
    public async Task<List<ClientEntity>> AddClientsToOrganizationAsync(Guid organizationId, int count)
    {
        await using var appDbContext = await _appDbContextFactory.CreateDbContextAsync();
        if (!await appDbContext.Organizations.AnyAsync(o => o.Id == organizationId))
        {
            _logger.LogWarning("Could not add clients: Organization with ID '{OrgId}' not found.", organizationId);
            return new List<ClientEntity>();
        }

        var clientFaker = new Faker<ClientEntity>()
            .RuleFor(c => c.ClientId, f => $"client-{f.Internet.DomainWord()}-{f.Random.Hexadecimal(8)}")
            .RuleFor(c => c.ClientName, f => f.Company.CompanyName())
            .RuleFor(c => c.Enabled, true)
            .RuleFor(c => c.ProtocolType, "oidc")
            .RuleFor(c => c.RequireClientSecret, false)
            .RuleFor(c => c.RequirePkce, true);

        var newClients = clientFaker.Generate(count);

        await using var configDbContext = await _configDbContextFactory.CreateDbContextAsync();
        await configDbContext.Clients.AddRangeAsync(newClients);
        await configDbContext.SaveChangesAsync();

        // Link clients to the organization
        foreach (var client in newClients)
        {
            appDbContext.ClientOwnerships.Add(new ClientOwnership { OrganizationId = organizationId, ClientId = client.Id });
        }
        await appDbContext.SaveChangesAsync();

        _logger.LogInformation("Successfully added {ClientCount} new clients to organization {OrgId}.", newClients.Count, organizationId);
        return newClients;
    }

    /// <summary>
    /// Resets a user's password to a new, random password.
    /// </summary>
    /// <param name="userIdentifier">The email or username of the user.</param>
    /// <returns>A tuple containing success status, the new password if successful, and any errors if failed.</returns>
    /// <usage>
    /// var (success, newPassword, errors) = await _dataHelper.ResetUserPasswordAsync("owner1@example.com");
    /// Console.WriteLine($"Password reset successfully! New password: {newPassword}");
    /// </usage>

    public async Task<(bool Success, string? NewPassword, IEnumerable<IdentityError>? Errors)> ResetUserPasswordAsync(string userIdentifier)
    {
        var user = await FindUserByIdentifierAsync(userIdentifier);
        if (user == null)
        {
            _logger.LogWarning("Could not reset password: User '{UserIdentifier}' not found.", userIdentifier);
            var errors = new[] { new IdentityError { Description = $"User '{userIdentifier}' not found." } };
            return (false, null, errors);
        }

        var newPassword = new Faker().Internet.Password(16, prefix: "Pass1!");
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (result.Succeeded)
        {
            _logger.LogInformation("Successfully reset password for user '{UserName}'.", user.UserName);
            return (true, newPassword, null);
        }

        _logger.LogWarning("Failed to reset password for user '{UserName}': {Errors}", user.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
        return (false, null, result.Errors);
    }

    private async Task<ApplicationUser?> FindUserByIdentifierAsync(string identifier)
    {
        if (identifier.Contains('@'))
        {
            return await _userManager.FindByEmailAsync(identifier);
        }
        return await _userManager.FindByNameAsync(identifier);
    }
}
