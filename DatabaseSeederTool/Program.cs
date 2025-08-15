using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using DatabaseSeederTool;

using Duende.IdentityServer.EntityFramework.DbContexts;

using OpenIdProvider.Data;
using OpenIdProvider.Data.Models;


var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((hostContext, services) =>
    {
        var connectionString = hostContext.Configuration.GetConnectionString("DefaultConnection");
        var migrationsAssembly = "OpenIdProvider.Data";

        services.AddLogging(configure => configure.AddConsole());

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly)));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddIdentityServer()
            .AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = b => b.UseNpgsql(connectionString,
                    sql => sql.MigrationsAssembly(migrationsAssembly));
            })
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = b => b.UseNpgsql(connectionString,
                    sql => sql.MigrationsAssembly(migrationsAssembly));
            });

        services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly)));
        services.AddDbContextFactory<ConfigurationDbContext>(options =>
            options.UseNpgsql(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly)));

        services.AddTransient<DatabaseSeeder>();
        services.AddTransient<DataHelperService>();
    })
    .Build();


using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Starting tool...");

    var seeder = services.GetRequiredService<DatabaseSeeder>();
    await seeder.AddStandardIdentityResources();

    // Using DatabaseSeeder
    // Uncomment the following lines to seed the database with realistic data.
    //var seeder = services.GetRequiredService<DatabaseSeeder>();
    //await seeder.SeedAsync(
    //    forceDelete: false,
    //    managersCount: 2,
    //    usersPerOrg: 50,
    //    groupsPerOrg: 5,
    //    clientsPerOrg: 3
    //);

    // Using DataHelper
    // Uncomment the following lines to create an organization for a specific user and add 5 users to it.
    //var dataHelper = services.GetRequiredService<DataHelperService>();
    //var newOrg = await dataHelper.CreateOrganizationForUserAsync("owner1@example.com");
    //if (newOrg != null)
    //{
    //    await dataHelper.AddUsersToOrganizationAsync(newOrg.Id, 5);
    //}

    logger.LogInformation("Tool finished its work.");
}
