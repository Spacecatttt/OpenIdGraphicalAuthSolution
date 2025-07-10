using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenIdProvider.Data.Models;
using OpenIdProvider.Data.ModelConfigurations;
using System.Reflection;

namespace OpenIdProvider.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupClaim> GroupClaims { get; set; }
    public DbSet<UserGroup> UserGroups { get; set; }
    public DbSet<Organization> Organizations { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        //Database.EnsureDeleted();
        //Console.WriteLine("Database has been deleted");
        //Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);


        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());


        //builder.ApplyConfiguration(new ApplicationUserConfiguration());
        //builder.ApplyConfiguration(new GroupConfiguration());
        //builder.ApplyConfiguration(new GroupClaimConfiguration());
        //builder.ApplyConfiguration(new UserGroupConfiguration());
        //builder.ApplyConfiguration(new OrganizationConfiguration());

    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseLazyLoadingProxies();
    }
}