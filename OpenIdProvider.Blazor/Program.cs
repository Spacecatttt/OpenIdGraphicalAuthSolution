using ApexCharts;
using Duende.IdentityModel.Client;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIdProvider.Blazor.Components;
using OpenIdProvider.Blazor.Components.Account;
using OpenIdProvider.Blazor.Endpoints;
using OpenIdProvider.Blazor.Services;
using OpenIdProvider.Data;
using OpenIdProvider.Data.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(ctx.Configuration));

// Blazor settings
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- Variables ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var migrationsAssembly = typeof(ApplicationDbContext).Assembly.GetName().Name;

// --- Authentication & Authorization Configuration ---

builder.Services.AddScoped<IAuthorizationHandler, ManagerUserHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IsManager", policy =>
        policy.Requirements.Add(new ManagerUserRequirement()));
});

// ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 10;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "OpenIdProvider.Blazor.Auth";
        options.LoginPath = "/account/login";
        // CRITICAL FOR REVERSE PROXIES: Ensures cookie is accessible and secure
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; 
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

// Duende IdentityServer
builder.Services.AddIdentityServer(options =>
    {
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseInformationEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseSuccessEvents = true;
        options.EmitStaticAudienceClaim = true;
    })
    .AddConfigurationStore(options =>
    {
        options.ConfigureDbContext = b => b.UseNpgsql(connectionString,
            sql => sql.MigrationsAssembly(migrationsAssembly));
    })
    .AddOperationalStore(options =>
    {
        options.ConfigureDbContext = b => b.UseNpgsql(connectionString,
            sql => sql.MigrationsAssembly(migrationsAssembly));
    })
    .AddAspNetIdentity<ApplicationUser>();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

// --- Database and Custom Services ---
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<IOrganizationResolver, OrganizationResolver>();
builder.Services.AddSingleton<IHelperService, HelperService>();
builder.Services.AddScoped<AppState>();

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly))
           .UseLazyLoadingProxies());

builder.Services.AddDbContextFactory<ConfigurationDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly)),
    ServiceLifetime.Scoped);

// Data Protection Configuration
var keysPath = Environment.GetEnvironmentVariable("DATAPROTECTION_KEYS_PATH");
if (string.IsNullOrEmpty(keysPath))
{
    keysPath = Path.Combine(builder.Environment.ContentRootPath, "dp_keys");
}
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

builder.Services.AddRazorPages();

// HTTP Client Setup
builder.Services.AddHttpClient();
builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
    }
    return new HttpClient(handler)
    {
        BaseAddress = new Uri(navigationManager.BaseUri)
    };
});

builder.Services.AddApexCharts(e =>
{
    e.GlobalOptions = new ApexChartBaseOptions
    {
        Debug = true,
        Theme = new Theme { Palette = PaletteType.Palette6 }
    };
});

var app = builder.Build();

// --- Middleware Pipeline Configuration ---

// FIX 1: Allow Forwarded Headers from any proxy (Required for Railway)
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

// Middleware to strictly enforce HTTPS scheme internally based on proxy headers
app.Use((context, next) =>
{
    if (context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) && proto == "https")
    {
        context.Request.Scheme = "https";
    }
    return next();
});

// Database Warmup Check
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        if (await dbContext.Database.CanConnectAsync())
        {
            app.Logger.LogInformation("Database connection successfully established and warmed up.");
        }
        else
        {
            app.Logger.LogWarning("Failed to connect to the database during warmup.");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while connecting to the database during warmup.");
    }
}

// Automatic Database Migrations with Retry Policy
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    int maxRetries = 5;
    int delayInSeconds = 5;

    for (int i = 1; i <= maxRetries; i++)
    {
        try
        {
            logger.LogInformation($"Database connection attempt {i} of {maxRetries}...");

            var appDbContext = services.GetRequiredService<ApplicationDbContext>();
            var configDbContext = services.GetRequiredService<ConfigurationDbContext>();
            var persistedGrantDbContext = services.GetRequiredService<PersistedGrantDbContext>();

            await appDbContext.Database.MigrateAsync();
            await configDbContext.Database.MigrateAsync();
            await persistedGrantDbContext.Database.MigrateAsync();

            logger.LogInformation("Migrations applied successfully.");

            if (!appDbContext.Users.Any())
            {
                logger.LogInformation("Database is empty. Running DatabaseSeeder...");
                // await DatabaseSeeder.SeedAsync(services);
                logger.LogInformation("Seeding completed successfully.");
            }

            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Attempt {i} failed: {ex.Message}");

            if (i == maxRetries)
            {
                logger.LogError(ex, "All migration attempts failed. Shutting down.");
                throw;
            }

            logger.LogInformation($"Waiting for {delayInSeconds} seconds before next attempt...");
            await Task.Delay(TimeSpan.FromSeconds(delayInSeconds));
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// FIX 2: Correct order. UseIdentityServer includes UseAuthentication inside itself.
app.UseIdentityServer(); 
app.UseAuthorization();

app.UseAntiforgery();

// --- Endpoint Mapping ---
app.MapRazorPages();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapAccountEndpoints();

await app.RunAsync();