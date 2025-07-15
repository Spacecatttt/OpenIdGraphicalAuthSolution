using OpenIdProvider.Blazor.Components;
using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIdProvider.Data;
using OpenIdProvider.Data.Models;
using OpenIdProvider.Blazor.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog (copied from old project)
builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(ctx.Configuration));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var migrationsAssembly = typeof(ApplicationDbContext).Assembly.GetName().Name;

// Add ASP.NET Core Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly))
             .UseLazyLoadingProxies());

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        // You can also move the password options here
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Register Duende IdentityServer
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

// here RequireConfirmedAccount
//builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
//    .AddEntityFrameworkStores<ApplicationDbContext>()
//    .AddSignInManager()
//    .AddDefaultTokenProviders();
//
//

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
});

//builder.Services.AddRazorPages(options =>
//{TODO
//    // Захищаємо всі сторінки в папці "Private"
//    // Це означає, що будь-яка Razor Page в /Pages/Private (і її підпапках)
//    // вимагатиме авторизації.
//    options.Conventions.AuthorizeFolder("/Private");
//
//    // Можна також захистити конкретну сторінку
//    // options.Conventions.AuthorizePage("/SecretPage");
//
//    // Або дозволити анонімний доступ до певної сторінки в захищеній папці
//    // options.Conventions.AllowAnonymousToPage("/Private/PublicInfo");
//});

// Add support for Razor Pages (for IdentityServer UI)
builder.Services.AddRazorPages();

builder.Services.AddScoped<IOrganizationResolver, OrganizationResolver>();

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

// Add IdentityServer to the pipeline (before Authorization)
app.UseIdentityServer();
app.UseAuthorization();
app.UseAntiforgery();

// ---Endpoint Mapping-- -
app.MapRazorPages();
// Map Blazor components for your main application
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

// Initialize IdentityServer
// SeedData.EnsureSeedData(app);
//AddData.EnsureSeedData(app);

app.Run();
