using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Bogus;

using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;

using ClientEntity = Duende.IdentityServer.EntityFramework.Entities.Client;

using IdentityResource = Duende.IdentityServer.Models.IdentityResource;

using OpenIdProvider.Data;
using OpenIdProvider.Data.Models;

namespace DatabaseSeederTool;

/// <summary>
/// A service to seed the database with a large volume of realistic test data using Bogus.
/// </summary>
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _appDbContext;
    private readonly ConfigurationDbContext _configDbContext;
    private readonly PersistedGrantDbContext _grantContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DatabaseSeeder> _logger;
    private const string Password = "Password123!";

    public DatabaseSeeder(
        ApplicationDbContext appDbContext,
        ConfigurationDbContext configDbContext,
        PersistedGrantDbContext grantContext,
        UserManager<ApplicationUser> userManager,
        ILogger<DatabaseSeeder> logger)
    {
        _appDbContext = appDbContext;
        _configDbContext = configDbContext;
        _grantContext = grantContext;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// The main method to execute the seeding process.
    /// </summary>
    public async Task SeedAsync(bool forceDelete = false, int managersCount = 2, int usersPerOrg = 100, int groupsPerOrg = 10, int clientsPerOrg = 5)
    {
        if (forceDelete)
        {
            _logger.LogInformation("Forcing database deletion and recreation...");
            await _appDbContext.Database.EnsureDeletedAsync();
            await _appDbContext.Database.MigrateAsync();
            await _configDbContext.Database.MigrateAsync();
            await _grantContext.Database.MigrateAsync();

            await AddStandardIdentityResources();
        }
        else if (await _appDbContext.Users.AnyAsync())
        {
            _logger.LogWarning("Database already contains data. Skipping seeding.");
            return;
        }

        _logger.LogInformation("Seeding database with large volume of test data using Bogus...");

        var managers = new List<ApplicationUser>();
        for (int i = 0; i < managersCount; i++)
        {
            var manager = await CreateManagerUserAsync($"owner{i + 1}@example.com");
            managers.Add(manager);
        }

        foreach (var manager in managers)
        {
            var orgId = manager.PrimaryOrganizationId;
            if (orgId == null) continue;

            _logger.LogInformation("Seeding data for organization of manager {ManagerEmail}", manager.Email);

            var groupFaker = new Faker<Group>()
                .RuleFor(g => g.Name, f => f.Commerce.Department())
                .RuleFor(g => g.Description, f => f.Lorem.Sentence())
                .RuleFor(g => g.OrganizationId, orgId.Value);

            var groups = groupFaker.Generate(groupsPerOrg);
            await _appDbContext.Groups.AddRangeAsync(groups);
            await _appDbContext.SaveChangesAsync();

            var clientFaker = new Faker<ClientEntity>()
                .RuleFor(c => c.ClientId, f => $"client-{f.Internet.DomainWord()}-{f.Random.Hexadecimal(8)}")
                .RuleFor(c => c.ClientName, f => f.Company.CompanyName())
                .RuleFor(c => c.Enabled, true)
                .RuleFor(c => c.ProtocolType, "oidc")
                .RuleFor(c => c.RequireClientSecret, false)
                .RuleFor(c => c.RequirePkce, true);

            var clients = clientFaker.Generate(clientsPerOrg);
            await _configDbContext.Clients.AddRangeAsync(clients);
            await _configDbContext.SaveChangesAsync();

            foreach (var client in clients)
            {
                _appDbContext.ClientOwnerships.Add(new ClientOwnership { OrganizationId = orgId.Value, ClientId = client.Id });
            }
            await _appDbContext.SaveChangesAsync();

            var userFaker = new Faker<ApplicationUser>()
                .RuleFor(u => u.DisplayName, f => f.Name.FullName())
                .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.DisplayName))
                .RuleFor(u => u.UserName, (f, u) => u.Email)
                .RuleFor(u => u.PrimaryOrganizationId, (Guid?)null)
                .RuleFor(u => u.EmailConfirmed, true);

            var endUsers = userFaker.Generate(usersPerOrg);
            var faker = new Faker();

            foreach (var user in endUsers)
            {
                await _userManager.CreateAsync(user, Password);
                _appDbContext.UserOrganizationRoles.Add(new UserOrganizationRole
                {
                    UserId = user.Id,
                    OrganizationId = orgId.Value,
                    Role = OrganizationRole.Member,
                    AddedDate = faker.Date.Recent(60, DateTime.UtcNow)
                });

                var userGroups = faker.PickRandom(groups, faker.Random.Int(1, 3)).ToList();
                foreach (var group in userGroups)
                {
                    user.Groups.Add(group);
                }
            }
            await _appDbContext.SaveChangesAsync();
        }

        _logger.LogInformation("Database seeding completed successfully.");
    }

    /// <summary>
    /// Creates a basic "IdentityResources".
    /// </summary>
    public async Task AddStandardIdentityResources()
    {
        if (await _configDbContext.IdentityResources.AnyAsync()) return;

        _logger.LogInformation("Adding standard Identity Resources...");
        var identityResources = new List<IdentityResource>
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email(),
            new IdentityResources.Phone(),
            new IdentityResources.Address(),
        };

        foreach (var resource in identityResources)
        {
            await _configDbContext.IdentityResources.AddAsync(resource.ToEntity());
        }
        await _configDbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a "Manager" user who owns their own primary organization.
    /// </summary>
    private async Task<ApplicationUser> CreateManagerUserAsync(string email)
    {
        var faker = new Faker();
        var displayName = faker.Name.FullName();

        var personalOrg = new Organization
        {
            Name = $"{displayName}'s Organization",
            Slug = $"{displayName.ToLower().Replace(" ", "-")}-{faker.Random.Hexadecimal(4)}",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        _appDbContext.Organizations.Add(personalOrg);
        await _appDbContext.SaveChangesAsync();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            PrimaryOrganizationId = personalOrg.Id,
            EmailConfirmed = true
        };
        await _userManager.CreateAsync(user, Password);

        _appDbContext.UserOrganizationRoles.Add(new UserOrganizationRole
        {
            UserId = user.Id,
            OrganizationId = personalOrg.Id,
            Role = OrganizationRole.Owner
        });
        await _appDbContext.SaveChangesAsync();

        return user;
    }
}
