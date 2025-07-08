// In OpenIdProvider.Web/SeedData.cs
using System.Security.Claims;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIdProvider.Data;
using OpenIdProvider.Data.Models;

public static class SeedData
{
    public static void EnsureSeedData(WebApplication app)
    {
        using (var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Database.Migrate();

            var configContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            configContext.Database.Migrate();


            if (!context.Organizations.Any())
            {
                context.Organizations.Add(new Organization
                {
                    Id = new Guid("11111111-1111-1111-1111-111111111111"),
                    Name = "Default Org"
                });
                context.SaveChanges();
            }

            // Seed clients
            if (!configContext.Clients.Any())
            {
                foreach (var client in Config.Clients)
                {
                    configContext.Clients.Add(client.ToEntity());
                }
                configContext.SaveChanges();
            }

            // Seed Identity Resources
            if (!configContext.IdentityResources.Any())
            {
                foreach (var resource in Config.IdentityResources)
                {
                    configContext.IdentityResources.Add(resource.ToEntity());
                }
                configContext.SaveChanges();
            }

            // Seed API Scopes
            if (!configContext.ApiScopes.Any())
            {
                foreach (var scopeResource in Config.ApiScopes)
                {
                    configContext.ApiScopes.Add(scopeResource.ToEntity());
                }
                configContext.SaveChanges();
            }

            // Seed a test user
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            if (userMgr.FindByNameAsync("alice").Result == null)
            {
                var alice = new ApplicationUser
                {
                    UserName = "alice",
                    Email = "AliceSmith@email.com",
                    EmailConfirmed = true,
                    OrganizationId = new Guid("11111111-1111-1111-1111-111111111111")
                };

                var result = userMgr.CreateAsync(alice, "Pass123$").Result;
                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }

                result = userMgr.AddClaimsAsync(alice, new Claim[]{
                    new Claim("name", "Alice Smith"),
                    new Claim("email", "AliceSmith@email.com")
                }).Result;
            }
        }
    }
}

// In-memory configuration for clients and resources
public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email(),
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new ApiScope("api1", "My API")
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            // Simple MVC client for testing
            new Client
            {
                ClientId = "mvc",
                ClientSecrets = { new Secret("secret".Sha256()) },
                AllowedGrantTypes = GrantTypes.Code,

                // where to redirect to after login
                RedirectUris = { "https://localhost:7002/signin-oidc" },

                // where to redirect to after logout
                PostLogoutRedirectUris = { "https://localhost:7002/signout-callback-oidc" },

                AllowedScopes = { "openid", "profile", "email", "api1" }
            }
        };
}