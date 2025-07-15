using System.Security.Claims;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIdProvider.Data;
using OpenIdProvider.Data.Models;

public static class AddData
{
    public static void EnsureSeedData(WebApplication app)
    {
        using var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var configContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

        //var org = CreateOrganization(context, "Org2", "org2");
        //var user = CreateUser(userMgr, "user2", "user2@example.com", "P@ssw0rd!", org.Id);
        CreateOrganizations(context, 20);

    }
    public static Organization CreateOrganization(ApplicationDbContext context, string name, string slug)
    {
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug
        };

        context.Organizations.Add(organization);
        context.SaveChanges();
        return organization;
    }

    public static ApplicationUser? CreateUser(UserManager<ApplicationUser> userMgr, string username, string email, string password, Guid organizationId)
    {
        var existingUser = userMgr.FindByNameAsync(username).Result;
        if (existingUser != null)
            return existingUser;

        var user = new ApplicationUser
        {
            UserName = username,
            Email = email,
            PrimaryOrganizationId = organizationId,
            DisplayName = username
        };

        var result = userMgr.CreateAsync(user, password).Result;
        return result.Succeeded ? user : null;
    }

    public static void CreateOrganizations(ApplicationDbContext context, int number)
    {
        for (int i = 1; i <= number; i++)
        {
            var name = $"Org{i}";
            var slug = $"org{i}";
            if (!context.Organizations.Any(o => o.Slug == slug))
            {
                CreateOrganization(context, name, slug);
            }
        }
    }
}
