using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using ApexCharts;

using Duende.IdentityModel.Client;

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

// Add authentication and authorization for application
builder.Services.AddAuthorization();

// ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "OpenIdProvider.Blazor.Auth";
        options.LoginPath = "/account/login";
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

// Provides the authentication state to Blazor components
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

// --- Database and My Services ---
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<IOrganizationResolver, OrganizationResolver>();
builder.Services.AddScoped<AppState>();

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly))
           .UseLazyLoadingProxies());

//
var keysPath = Environment.GetEnvironmentVariable("DATAPROTECTION_KEYS_PATH");
if (string.IsNullOrEmpty(keysPath))
{
    // if not exist path -> create it
    keysPath = Path.Combine(builder.Environment.ContentRootPath, "dp_keys");
}
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

// Add support for Razor Pages (for IdentityServer UI)
builder.Services.AddRazorPages();

builder.Services.AddHttpClient();
builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };
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

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        if (dbContext.Database.CanConnect())
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();

//
app.UseAuthentication();
app.UseIdentityServer();
app.UseAuthorization();

app.UseAntiforgery();

// ---Endpoint Mapping-- -
app.MapRazorPages();
// Map Blazor components for your main application
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.MapAccountEndpoints();

// Initialize IdentityServer
// SeedData.EnsureSeedData(app);
// AddData.EnsureSeedData(app);

app.Run();
