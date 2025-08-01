using Bogus;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIdProvider.Data;
using OpenIdProvider.Data.Models;
using ClientEntity = Duende.IdentityServer.EntityFramework.Entities.Client;

namespace OpenIdProvider.Blazor.Services;

/// <summary>
/// A service to seed the database with a large volume of realistic test data using Bogus.
/// </summary>
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _appDbContext;
    private readonly ConfigurationDbContext _configDbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DatabaseSeeder> _logger;
    private const string Password = "Password123!";

    public DatabaseSeeder(
        ApplicationDbContext appDbContext,
        ConfigurationDbContext configDbContext,
        UserManager<ApplicationUser> userManager,
        ILogger<DatabaseSeeder> logger)
    {
        _appDbContext = appDbContext;
        _configDbContext = configDbContext;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// The main method to execute the seeding process.
    /// </summary>
    public async Task SeedAsync(bool forceDelete = false, int managersCount = 2, int usersPerOrg = 100, int groupsPerOrg = 10, int clientsPerOrg = 5)
    {
        // If we want to clean the database before seeding
        if (forceDelete)
        {
            _logger.LogInformation("Forcing database deletion and recreation...");
            await _appDbContext.Database.EnsureDeletedAsync();
            await _appDbContext.Database.MigrateAsync();
            await _configDbContext.Database.MigrateAsync();
        }
        else if (await _appDbContext.Users.AnyAsync())
        {
            _logger.LogWarning("Database already contains data. Skipping seeding.");
            return;
        }

        _logger.LogInformation("Seeding database with large volume of test data using Bogus...");

        // Create Manager Users and their Primary Organizations
        var managers = new List<ApplicationUser>();
        for (int i = 0; i < managersCount; i++)
        {
            var manager = await CreateManagerUserAsync($"owner{i + 1}@example.com");
            managers.Add(manager);
        }

        // For each manager's organization, generate users, groups, and clients
        foreach (var manager in managers)
        {
            var orgId = manager.PrimaryOrganizationId;
            if (orgId == null) continue;

            // --- Generate Groups ---
            var groupFaker = new Faker<Group>()
                .RuleFor(g => g.Name, f => f.Commerce.Department())
                .RuleFor(g => g.Description, f => f.Lorem.Sentence())
                .RuleFor(g => g.OrganizationId, orgId.Value);

            var groups = groupFaker.Generate(groupsPerOrg);
            await _appDbContext.Groups.AddRangeAsync(groups);
            await _appDbContext.SaveChangesAsync();

            // --- Generate Clients ---
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

            // Link clients to the organization
            foreach (var client in clients)
            {
                _appDbContext.ClientOwnerships.Add(new ClientOwnership { OrganizationId = orgId.Value, ClientId = client.Id });
            }
            await _appDbContext.SaveChangesAsync();

            // --- Generate End-Users ---
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

                // Add user to the organization with a 'Member' role
                _appDbContext.UserOrganizationRoles.Add(new UserOrganizationRole
                {
                    UserId = user.Id,
                    OrganizationId = orgId.Value,
                    Role = OrganizationRole.Member,
                    AddedDate = faker.Date.Recent(60, DateTime.UtcNow)
                });

                // Randomly assign user to some groups
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
        return user;
    }
}
